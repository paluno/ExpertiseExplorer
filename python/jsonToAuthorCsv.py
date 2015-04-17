import json
import codecs
import sys
from pprint import pprint
from StringIO import StringIO

unknown = "unknown"
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
                    
            
            
           
            currentRevision = revisions[currentRevisionId]
            
            if not currentRevision.has_key("commit"):
                sys.stderr.write("No commit in line " + str(current_line_number))
                continue
            
            authorEmail = currentRevision["commit"]["author"]["email"]
            date = currentRevision["commit"]["committer"]["date"]
            

            files = currentRevision["files"]
            filenames = ""
            concatChar = ""
            
            for file in files:
                filename = "" + file
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
        