import sys

f = open(sys.argv[1], 'r')
result = open(sys.argv[2], "w")

print sys.argv[1]
print sys.argv[2]

braceCounter = 0
escaped = False
lastEscapeChar = False
firstChar = True

while True:
	ch=f.read(1)
	if not ch: 
		break
		
	if firstChar:
		firstChar = False
		continue
	
	result.write(ch)
	
	if ch == '"' and not lastEscapeChar: escaped = (not escaped)
	
	if ch == '\\': lastEscapeChar = not lastEscapeChar
	if not ch == '\\': lastEscapeChar = False
	
	if escaped: continue
	
	if ch == '{': 
		braceCounter += 1
	if ch == '}': 
		braceCounter -= 1
		if braceCounter == 0:
			result.write('\n')
			firstChar = True
f.close()
result.close()
	
	