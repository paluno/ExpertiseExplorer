$utcDateString = "2010-05-03 21:28:31Z"

$utcDate = [DateTime]::Parse($utcDateString).ToUniversalTime()
$startOfTime = new-object DateTime(1970,1,1,0,0,0,[DateTimeKind]::Utc)
$unixTime = $utcDate.Subtract($startOfTime).TotalSeconds

$unixTime
