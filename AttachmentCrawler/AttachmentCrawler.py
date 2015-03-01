import codecs
import hashlib
import httplib
import multiprocessing
import os
import re
import sys
import time

SITE = 'bug%s.bugzilla.mozilla.org'
BASEURL = '/attachment.cgi?id=%s&action=diff&collapsed=&context=patch&format=raw&headers=1'
THREADSNUMBER = 1

class AttachmentCrawler(multiprocessing.Process):

    def __init__(self, basepath, filename, id):
        multiprocessing.Process.__init__(self)
        self.id = id
        self.basepath = basepath
        self.filename = basepath + filename
        self.totalnumberoflines = 0
        self.currentnumberoflines = 0
        
    def getHtmlFile(self, site, page):
        try:
            httpconn = httplib.HTTPSConnection(site)
            httpconn.request("GET", page)
            resp = httpconn.getresponse()
            resppage = resp.read()
        except:
            resppage = None

        return resppage

    def getLinkFromLine(self, line):
        start = line.index('https://github.com/')
        end = line.find('"', start)
        if end == -1:
            end = line.find('\'', start)
            if end == -1:
                end = line.find('\n', start)
                if end == -1:
                    end = len(line)
        result = line[start:end]
        if "/files" not in result:
            result += "/files"
        return result

    def getFilesFromGithub(self, htmlpage):
        filenames = []
        htmldata = htmlpage.split('\n')
        for line in htmldata:
            if line.find('<div class="meta clearfix" data-path="') > -1:
                start = line.index('<div class="meta clearfix" data-path="') + 38
                end = line.find('">', start)
                names = line[start:end].split(' ')
                for name in names:
                    if name not in filenames:
                        filenames.append(name)
        return filenames

    def getAttachmentFromLine(self, line):
        bugID, attachmentID = self.getValuesFromLine(line)
        site = SITE % (bugID)
        page = BASEURL % (attachmentID)
        html = self.getHtmlFile(site, page)
        if html is None:
            failcount = 1
            while failcount < 4:
                time.sleep(failcount * failcount + failcount)
                html = self.getHtmlFile(site, page)
                if html:
                    break
                else:
                    failcount += 1

        filenames = self.parse(html)
        print >> self.outfile, "%s;%s;%s" % (bugID, attachmentID, ",".join(filenames))
        self.outfile.flush();
        
    def getValuesFromLine(self, line):
        bugID = line.split(";")[0]
        attachmentID = line.split(";")[4]
        attachmentID = attachmentID.replace("attachment #", "")
        attachmentID = attachmentID.replace("flags", "")
        return (bugID, attachmentID.strip())

    def parse(self, htmlfile):
        filenames = []
        lines = htmlfile.split("\n")
        for line in lines:
            if line.startswith("+++ "):
                cleanline = line.replace("\t", " ")
                diffData = cleanline.split(" ")
                filename = diffData[1].strip()
                
                if '/dev/null' in filename:
                    continue

                #if filename.find("/") != -1:
                #	filename = filename[filename.find("/"):]

                if filename not in filenames:
                    filenames.append(filename)
                continue
            
            if line.startswith("RCS file: "):
                cleanline = line.replace("\t", " ")
                cleanline = cleanline.replace("RCS file: ", "")
                cleanline = cleanline.replace("/cvsroot/mozilla", "")
                cleanline = cleanline.replace("/cvs/mozilla", "")
                rcsData = cleanline.split(',')
                filename = rcsData[0].strip()
                if filename not in filenames:
                    filenames.append(filename)
                continue

            if line.startswith("Index: "):
                cleanline = line.replace("\t", " ")
                cleanline = cleanline.replace("Index: ", "")
                #cleanline = cleanline.replace("mozilla", "")
                filename = cleanline.strip()
                if filename not in filenames:
                    filenames.append(filename)
                continue

            if 'https://github.com/' in line:
                if '/pull/' in line:
                    #print 'PULL REQUEST FROM: %s' % (line)
                    originallink = self.getLinkFromLine(line)
                    site = "github.com"
                    page = originallink.replace('https://github.com', '')
                    html = self.getHtmlFile(site, page)
                    if html is None:
                        failcount = 1
                        while failcount < 4:
                            time.sleep(failcount * failcount + failcount)
                            html = self.getHtmlFile(site, page)
                            if html:
                                break
                            else:
                                failcount += 1
                    
                    names = self.getFilesFromGithub(html)
                    for name in names:
                        if name not in filenames:
                            filenames.append(name)
                continue

        return filenames

    def run(self):
        self.totalnumberoflines = file_len(self.filename)
        print 'Thread %s: %s lines total' % (self.id, self.totalnumberoflines)
        self.infile = open(self.filename, 'r')
        self.outfile = open(self.basepath + "CrawlerOutput\\output%s.txt" % (self.id), 'a+')
        for line in self.infile:
            self.getAttachmentFromLine(line)
            self.currentnumberoflines += 1
            if self.currentnumberoflines % 1000 == 0:
                print 'Thread %s: %s/%s' % (self.id, self.currentnumberoflines, self.totalnumberoflines)

        self.outfile.close()
        self.infile.close()
        print 'Thread %s: finished!' % (self.id)

def file_len(fname):
    with open(fname) as f:
        for i, l in enumerate(f):
            pass
    return i + 1

def preparefiles(basepath, basefilename):
    numberoflines = file_len(basepath + basefilename)
    linesperfile = numberoflines / THREADSNUMBER
    rest = numberoflines % THREADSNUMBER
    with open(basepath + basefilename, 'r') as basefile:
        writtenlines = 0
        for i in range(THREADSNUMBER):
            with open(basepath + "attachment_crawler_part_%s.txt" % (i), 'a+') as part:
                while (writtenlines < linesperfile * (i + 1)):
                    line = basefile.readline()
                    part.write(line)
                    writtenlines += 1
        # add remainder to last file
        if rest > 0:
            with open(basepath + "attachment_crawler_part_%s.txt" % (THREADSNUMBER - 1), 'a+') as part:
                for i in range(rest):
                    line = basefile.readline()
                    part.write(line)

def main():
    if len(sys.argv) < 3:
        print "Usage:python AttachmentCrawler.py basepath filename"
        sys.exit(1)

    basepath = sys.argv[1]
    filename = sys.argv[2]

    # prepare x file parts
    preparefiles(basepath, filename)

    # create output directory
    if not os.path.exists(basepath + "CrawlerOutput\\"):
        os.makedirs(basepath + "CrawlerOutput\\")

    # create crawler threads
    threadlist = []
    for i in range(THREADSNUMBER):
        crawler = AttachmentCrawler(basepath, "attachment_crawler_part_%s.txt" % (i), i)
        threadlist.append(crawler)
    
    # start threads
    for thread in threadlist:
        thread.start()

    # wait for all threads to finish
    for thread in threadlist:
        thread.join()

    # combine the files again
    with open(basepath + "CrawlerOutput\\combinedoutput.txt", 'a+') as outfile:
        for i in range(THREADSNUMBER):
            for line in open(basepath + "CrawlerOutput\\output%s.txt" % (i), 'r'):
                outfile.write(line)
    
    # remove duplicate lines
    lines_seen = set() # holds lines already seen
    with open(basepath + "CrawlerOutput\\combinedoutput_reduced.txt", "a+") as outfile:
        for line in open(basepath + "CrawlerOutput\\combinedoutput.txt", "r"):
            hash = hashlib.md5(line).hexdigest()
            if hash not in lines_seen: # not a duplicate
                outfile.write(line)
                lines_seen.add(hash)

if __name__ == "__main__":
  main()