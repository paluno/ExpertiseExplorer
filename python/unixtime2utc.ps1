﻿$unixTime = 1279543518;

$startOfTime = new-object DateTime(1970,1,1,0,0,0,[DateTimeKind]::Utc);

$convertedTime = $startOfTime.AddSeconds($unixTime);

$convertedTime.ToString("u");