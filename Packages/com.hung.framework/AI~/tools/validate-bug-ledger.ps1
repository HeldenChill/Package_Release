param(
    [Parameter(Mandatory = $false)]
    [string]$LedgerPath = '',
    # Comma-separated IDs of rows already known to be malformed (the project lists them in project-values.md).
    [string]$KnownMalformedIds = '',
    [string]$ProjectPath = '.',
    [string]$RecordsDirectory = ''
)

# Ledger validator adapter.
# If shared validator exists in project, delegates directly to Node.js CLI.
# Otherwise falls back to legacy single-file PowerShell validator with full-file validation.
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
$validator = Join-Path $projectRoot '.claude/tools/ai-audit/validate-bug-memory.js'
if (Test-Path -LiteralPath $validator) {
    $argsForNode = @($validator, '--project', $projectRoot)
    if ($LedgerPath) { $argsForNode += @('--ledger', $LedgerPath) }
    if ($KnownMalformedIds) { $argsForNode += @('--known-malformed-ids', $KnownMalformedIds) }
    if ($RecordsDirectory) { $argsForNode += @('--records-dir', $RecordsDirectory) }
    & node @argsForNode
    exit $LASTEXITCODE
}

# Resolve values from project-values.md if LedgerPath not passed
$valuesFile = Join-Path $projectRoot '.claude/rules/project-values.md'
$isCollection = $false
if (-not $LedgerPath -and (Test-Path -LiteralPath $valuesFile)) {
    $valuesContent = Get-Content -LiteralPath $valuesFile -Raw
    if ($valuesContent -match 'BUG_RECORDS_DIR\s*=\s*([^\r\n]+)') {
        $RecordsDirectory = $matches[1].Trim()
        $isCollection = $true
    }
    if ($valuesContent -match 'BUG_LEDGER\s*=\s*([^\r\n]+)') {
        $LedgerPath = $matches[1].Trim()
    }
}
if ($RecordsDirectory) { $isCollection = $true }

if ($isCollection) {
    throw "Collection mode requires shared validator at $validator. Run sync.ps1 pull to install."
}

if (-not $LedgerPath) {
    $LedgerPath = '.cursor/memory/mem-known-bugs-index.md'
}

$fullLedgerPath = Join-Path $projectRoot $LedgerPath
if (-not (Test-Path -LiteralPath $fullLedgerPath)) { throw "Ledger not found: $fullLedgerPath" }
$content = Get-Content -LiteralPath $fullLedgerPath

$allowedTypes = @('bug', 'risk', 'gap', 'planned')
$allowedSeverities = @('critical', 'high', 'medium', 'low', 'unknown')
$allowedStatuses = @(
    'SUSPECTED', 'CONFIRMED', 'IN_PROGRESS', 'VERIFY_PENDING', 'RESOLVED',
    'REJECTED', 'DEFERRED', 'WONT_FIX', 'DUPLICATE'
)
$terminalWithReason = @('RESOLVED', 'REJECTED', 'WONT_FIX', 'DUPLICATE')
$idPattern = 'BUG-(\d{4}|\d{6}-[0-9a-f]{4})'
$knownMalformed = @($KnownMalformedIds -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })

function Split-LedgerRow([string]$row) {
    $cells = New-Object System.Collections.Generic.List[string]
    $cur = New-Object System.Text.StringBuilder
    $code = $false
    $text = $row.Trim().Trim('|')
    for ($i = 0; $i -lt $text.Length; $i++) {
        $c = $text[$i]
        if ($c -eq '`') { $code = -not $code }
        if ($c -eq '|' -and -not $code -and ($i -eq 0 -or $text[$i - 1] -ne '\')) {
            $cells.Add($cur.ToString().Trim()); [void]$cur.Clear()
        } else { [void]$cur.Append($c) }
    }
    $cells.Add($cur.ToString().Trim())
    return , @($cells)
}

$inHistorical = $false
$inLedger = $false
$rows = @()
foreach ($line in $content) {
    if ($line -match '^##\s*(?:ID renumbering|Historical ID Mapping|Renumbering)') { $inHistorical = $true; $inLedger = $false; continue }
    if ($line -match '^##\s*Ledger\s*$') { $inLedger = $true; $inHistorical = $false; continue }
    if ($line -match '^##\s+') { $inLedger = $false; $inHistorical = $false }

    if ($inHistorical) { continue }

    if ($line -match "^\|\s*$idPattern\s*\|") {
        if (-not $inLedger) {
            throw "Record found outside canonical ## Ledger table: $line"
        }
        $rows += $line
    }
}
if ($rows.Count -eq 0) { throw 'No ledger rows found under ## Ledger' }

$seen = @{}
$records = @()
$warnings = New-Object System.Collections.Generic.List[string]
foreach ($row in $rows) {
    $cells = Split-LedgerRow $row
    $rowId = $cells[0]
    if ($seen.ContainsKey($rowId)) { throw "Duplicate ID: $rowId" }
    $seen[$rowId] = $true
    if ($cells.Count -ne 10) {
        if ($rowId -in $knownMalformed) { $warnings.Add("$rowId malformed (known, $($cells.Count) cells)"); continue }
        throw "Expected 10 columns on $rowId (got $($cells.Count)); escape embedded pipes: $row"
    }
    $record = [pscustomobject]@{
        Id = $cells[0]; Type = $cells[1]; Severity = $cells[2]; Status = $cells[3]
        Title = $cells[4]; Source = $cells[5]; Evidence = $cells[6]
        Found = $cells[7]; Updated = $cells[8]; Resolution = $cells[9]
    }
    $record.Status = $record.Status -replace '^<span class="st-[a-z]+">([A-Z_]+)</span>$', '$1'
    if ($record.Type -notin $allowedTypes) { $warnings.Add("$rowId invalid type: $($record.Type)") }
    if ($record.Severity -notin $allowedSeverities) { $warnings.Add("$rowId invalid severity: $($record.Severity)") }
    if ($record.Status -notin $allowedStatuses) { $warnings.Add("$rowId invalid status: $($record.Status)") }
    foreach ($date in @($record.Found, $record.Updated)) {
        if ($date -ne 'unknown' -and $date -notmatch '^\d{4}-\d{2}-\d{2}$') { $warnings.Add("$rowId invalid date cell: $($date.Substring(0, [Math]::Min(30, $date.Length)))") }
    }
    if ([string]::IsNullOrWhiteSpace($record.Title) -or [string]::IsNullOrWhiteSpace($record.Source) -or [string]::IsNullOrWhiteSpace($record.Evidence)) { $warnings.Add("$rowId missing core evidence field") }
    if ($record.Status -in $terminalWithReason -and $record.Resolution -in @('', '—', '-')) { $warnings.Add("$rowId terminal state lacks resolution") }
    $records += $record
}

foreach ($record in $records | Where-Object Status -eq 'DUPLICATE') {
    $target = [regex]::Match($record.Resolution, $idPattern).Value
    if (-not $target -or -not $seen.ContainsKey($target)) { $warnings.Add("$($record.Id) invalid duplicate target") }
}

if (($content -join "`n") -match 'Item resolved/fixed.*delete') { throw 'Delete-on-fix policy remains' }
foreach ($w in $warnings) { Write-Warning $w }
"PASS: $($records.Count) unique ledger records ($($warnings.Count) legacy warnings)"
