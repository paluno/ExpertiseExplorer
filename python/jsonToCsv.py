import json
import codecs
import sys
from pprint import pprint
from StringIO import StringIO

unknown = "unknown"
num_lines = sum(1 for line in open(sys.argv[1]))
current_line_number = 0

f = open(sys.argv[2],'w')

with codecs.open(sys.argv[1], "r", "utf-8") as jsonFile:
    for line in jsonFile:
        jsondata = json.loads("".join(line))
		
        result = {}
        
        messages = jsondata["messages"]
        revisions = jsondata["revisions"]
        
        owner = jsondata["owner"]
        
        authorEmail = unknown
        if owner.has_key("email"): authorEmail = owner["email"]
        
        authorId = owner["_account_id"]
        project = jsondata["project"]
        changeId = jsondata["change_id"]
        
        current_line_number += 1
        print "" + str(current_line_number) + "/" + str(num_lines)
        
        for message in messages:    
            text = message["message"]
            if "Code-Review+" not in text:
                if "Code-Review-" not in text:
                    continue
        
            date = message["date"]
            messageRevisionNumber = message["_revision_number"]
            messageId = message["id"]
            author = message["author"]
            
            reviewerEmail = unknown
            if author.has_key("email"): reviewerEmail = author["email"]
            reviewerId = author["_account_id"]
            
            for revisionName in revisions:
                revision = revisions[revisionName]
                revisionNumber = revision["_number"]
                if revisionNumber != messageRevisionNumber: continue
                
                revisionId = "" + revisionName
                files = revision["files"]
                filenames = ""
                concatChar = ""

                newFilenames = ""
                newFilenamesConcatChar = ""
				
                for file in files:
                    filename = "" + file
                    linesInserted = 0
                    if files[file].has_key("lines_inserted"): linesInserted = files[file]["lines_inserted"]
					
                    linesDeleted = 0
                    if files[file].has_key("lines_deleted"): linesDeleted = files[file]["lines_deleted"]
					
                    filename += ":" + str(linesInserted) + ":" + str(linesDeleted)
                    filenames += concatChar + filename
                    concatChar = ","
					
                    if files[file].has_key("status"):
                        filestatus = files[file]["status"]
                        if filestatus == "A":
                            newFilenames += newFilenamesConcatChar + str(file)
                            newFilenamesConcatChar = ","
					
                    
            resultArray = []
            resultArray.extend([project, changeId, date, authorEmail, authorId, revisionId, filenames, newFilenames, reviewerEmail,reviewerId])
            
            resultLine = ""
            concatChar = ""
            for resultItem in resultArray:
                resultLine += concatChar + str(resultItem)
                concatChar = ";"
            f.write(resultLine + "\n")
f.close()