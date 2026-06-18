# 1. Im Cycle-Folder (z.B. 'C:\Users\perfe\AppData\Local\pFemme\Storage\16BB2DC8E0C6BB82\Cycles') müssen die Dateien liegen, damit sie von der App geladen werden können. Daher wird dieses Skript im Cycle-Folder ausgeführt.
# 2. Stelle sicher, dass die Datei "CyclesData.sql" im selben Verzeichnis wie dieses Skript liegt.
# 3. Führe dieses Skript (PowerShell) aus, um die Daten aus der SQL-Datei zu extrahieren und in JSON-Dateien zu konvertieren.
# 4. Sicherstellen dass User-UnixTS den UnixTS der AuthUser entspricht, damit die Dateien in der App korrekt geladen werden können.

# Konfiguration
$sqlFile = "CyclesData.sql"

$content = Get-Content $sqlFile -Raw

# Regex-Pattern für die SQL-Daten
$regexPattern = @'
\((?<ID>\d+),\s*N'(?<UnixTS>[^']+)',\s*N'(?<AuthTS>[^']+)',\s*CAST\(N'(?<RecordDate>[^']*)'\s*AS\s*DateTime\),\s*N'(?<Details>[^']*)',\s*(?<bleeding>\d+),\s*(?<intensity>\d+),\s*(?<pain>\d+),\s*(?<headache>\d+),\s*(?<fatigue>\d+),\s*(?<nausea>\d+),\s*(?<cramps>\d+),\s*GETDATE\(\),\s*GETDATE\(\),\s*(?<LastUpdate>\d+)\)
'@

$matches = [regex]::Matches($content, $regexPattern)

foreach ($match in $matches) {
     $cycle = [PSCustomObject]@{
         ID               = [int]$match.Groups['ID'].Value
         UnixTS           = $match.Groups['UnixTS'].Value
         AuthUsers_UnixTS = $match.Groups['AuthTS'].Value
         RecordDate       = $match.Groups['RecordDate'].Value
         Details          = $match.Groups['Details'].Value
         
         # bleeding ist bool im Modell -> Konvertierung zu [bool]
         bleeding         = ([bool][int]$match.Groups['bleeding'].Value)
         
         # intensity, pain, headache, fatigue, nausea, cramps sind int im Modell -> Konvertierung zu [int]
         intensity        = [int]$match.Groups['intensity'].Value
         pain             = [int]$match.Groups['pain'].Value
         headache         = [int]$match.Groups['headache'].Value
         fatigue          = [int]$match.Groups['fatigue'].Value
         nausea           = [int]$match.Groups['nausea'].Value
         cramps           = [int]$match.Groups['cramps'].Value
         
         LastUpdateUnixTS = [long]$match.Groups['LastUpdate'].Value
     }

     $json = $cycle | ConvertTo-Json -Compress
     $fileName = "$($cycle.UnixTS).json"
     $json | Out-File -FilePath $fileName -Encoding utf8
}

Write-Host "Fertig! $($matches.Count) Dateien wurden im aktuellen Verzeichnis generiert."