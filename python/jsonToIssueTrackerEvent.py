import json
import codecs
import sys
from pprint import pprint
from StringIO import StringIO

def arrayToString(array):
    resultLine = ""
    concatChar = ""
    for resultItem in array:
        resultLine += concatChar + unicode(resultItem)
        concatChar = ";"
    return resultLine + "\n"

def getFilenames(files):
    filenames = ""
    concatChar = ""
    for file in files:
        filename = "" + file
        filename = filename.replace(":", "_")
        filenames += concatChar + filename
        concatChar = ","
    return filenames

def isReview(message):
    text = message["message"]
    if "Code-Review+" not in text:
        if "Code-Review-" not in text:
            if "Looks good to me, approved" not in text:
                return False
    
    return message.has_key("_revision_number")
    
def getAllCommits(jsondata):
    result = []
    revisions = jsondata["revisions"]
    changeId = jsondata["change_id"]
    
    for revisionId in revisions:
        revision = revisions[revisionId]
        date = revision["commit"]["committer"]["date"]
        
        if not revision.has_key("files"): continue  
        files = revision["files"]
        filenames = getFilenames(files)
        
        resultArray = []
        resultArray.extend([date, "c", changeId, filenames])
        resultString = arrayToString(resultArray).encode('utf-8')
        result.append(resultString)
    return result

def getAllReviews(jsondata):
    result = []

    changeId = jsondata["change_id"]       
    messages = jsondata["messages"]
    revisions = jsondata["revisions"]
        
    for message in messages:
        if not isReview(message): continue
        date = message["date"]
        messageRevisionNumber = message["_revision_number"]
        author = message["author"]
            
        reviewerEmail = unknown
        if author.has_key("email"): reviewerEmail = author["email"]
            
        for revisionName in revisions:
            revision = revisions[revisionName]
            revisionNumber = revision["_number"]
            if revisionNumber != messageRevisionNumber: continue

            if not revision.has_key("files"): continue
                
            files = revision["files"]
            filenames = getFilenames(files)       
            resultArray = []
            resultArray.extend([date, "r", changeId, filenames, reviewerEmail, revisionNumber])
            result.append(arrayToString(resultArray))    
    return result    

# main    
unknown = "unknown"
num_lines = sum(1 for line in open(sys.argv[1]))
current_line_number = 0

f = open(sys.argv[2],'w')

with codecs.open(sys.argv[1], "r", "utf-8") as jsonFile:
    for line in jsonFile:
        jsondata = json.loads("".join(line))
        current_line_number += 1
        print "" + str(current_line_number) + "/" + str(num_lines)
        
        result = []
        
        result.extend(getAllCommits(jsondata))
        result.extend(getAllReviews(jsondata))
        sortedResult = []
        for aResult in result:   
            date = aResult.split(";")[0]
            sortedResult.append([date, aResult])
        
        sortedResult.sort(key=lambda item: item[0])
        
        for aSortedResult in sortedResult:
            f.write(aSortedResult[1])
 
f.close()