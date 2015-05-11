$unixTime = 1174625168;

$startOfTime = new-object DateTime(1970,1,1,0,0,0,[DateTimeKind]::Utc);
$sevenHours = new-object TimeSpan(0,7,0,0);

$pdtTime = $startOfTime.AddSeconds($unixTime).Subtract($sevenHours);

$pdtTime;