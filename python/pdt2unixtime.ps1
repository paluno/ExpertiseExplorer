$pdtDateString = "12.11.2012 21:07:59"

$pdtDate = [DateTime]::Parse($pdtDateString).AddHours(7)
$startOfTime = new-object DateTime(1970,1,1,0,0,0,[DateTimeKind]::Utc)
$unixTime = $pdtDate.Subtract($startOfTime).TotalSeconds

$unixTime
