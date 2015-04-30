import json
import codecs
import sys
import datetime
from pprint import pprint
from StringIO import StringIO

def isMergeMessage(messageText):
    if "successfully merged into" not in messageText:
        if "successfully cherry" not in messageText:
            if "successfully pushed" not in messageText:
                return False
    return True


unknown = "unknown"
num_lines = 0
num_lines = sum(1 for line in open(sys.argv[1]))
current_line_number = 0

with codecs.open(sys.argv[2], "w", "utf-8") as f:
    with codecs.open(sys.argv[1], "r", "utf-8") as jsonFile:
        for line in jsonFile:
            jsondata = json.loads("".join(line))
            
            result = {}
            
            current_line_number += 1
            print "" + str(current_line_number) + "/" + str(num_lines)
            
            status = jsondata["status"]
            if status != "MERGED": continue
            
            revisions = jsondata["revisions"]
            currentRevisionId = ""
            
            if jsondata.has_key("current_revision"): currentRevisionId = jsondata["current_revision"]
            else:
                maxNumber = 0
                for revision in revisions:
                    number = revisions[revision]["_number"]
                    if number > maxNumber:
                        maxNumber = number
                        currentRevisionId = revision
           
            if not jsondata.has_key("messages"):
                sys.stderr.write("No messages in line " + str(current_line_number) + "\n")
                continue;
           
            messages = jsondata["messages"]
            
            if len(messages) == 0:
                sys.stderr.write("0 messages in line " + str(current_line_number) + "\n")
                continue;  
            
            newestMessage = ""
            newestMessage = messages[0]
            
            for message in messages:
                text = message["message"]
                if not isMergeMessage(text):
                    continue
                
                newestDate = datetime.datetime.strptime(newestMessage["date"], "%Y-%m-%d %H:%M:%S.%f000")
                currentMessageDate = datetime.datetime.strptime(message["date"], "%Y-%m-%d %H:%M:%S.%f000")
                if currentMessageDate > newestDate:
                    newestMessage = message
                        
            
            date = newestMessage["date"]
            
            if not isMergeMessage(newestMessage["message"]):
                sys.stderr.write("No successfully merged into in line " + str(current_line_number) + "\n")
            
           
            currentRevision = revisions[currentRevisionId]
            
            if not currentRevision.has_key("commit"):
                sys.stderr.write("No commit in line " + str(current_line_number) + "\n")
                continue
            
            authorEmail = currentRevision["commit"]["author"]["email"]
            
            

            files = currentRevision["files"]
            filenames = ""
            concatChar = ""
            
            for file in files:
                filename = "" + file
                filename = filename.replace(":", "_")
                linesInserted = 0
                if files[file].has_key("lines_inserted"): linesInserted = files[file]["lines_inserted"]
                
                linesDeleted = 0
                if files[file].has_key("lines_deleted"): linesDeleted = files[file]["lines_deleted"]
                
                isNewFile = False
                if files[file].has_key("status"):
                    filestatus = files[file]["status"]
                    if filestatus == "A":
                        isNewFile = True
                
                filename += ":" + str(linesInserted) + ":" + str(linesDeleted) + ":" + str(isNewFile)
                filenames += concatChar + filename
                concatChar = ","

            resultArray = []
            resultArray.extend([currentRevisionId, date, authorEmail, filenames])
            
            resultLine = ""
            concatChar = ""
            for resultItem in resultArray:
                resultLine += concatChar + unicode(resultItem)
                concatChar = ";"
            f.write(resultLine + "\n")