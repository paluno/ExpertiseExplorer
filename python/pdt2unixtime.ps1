$pdtDateString = "28.04.2009 16:33:15"

$pdtDate = [DateTime]::Parse($pdtDateString).AddHours(7)
$startOfTime = new-object DateTime(1970,1,1,0,0,0,[DateTimeKind]::Utc)
$unixTime = $pdtDate.Subtract($startOfTime).TotalSeconds

$unixTime
