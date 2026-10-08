param(
    [Parameter(Mandatory = $false)]
    [string]$LedgerPath = '.cursor/memory/mem-known-bugs-index.md',
    # Comma-separated IDs of rows already known to be malformed (the project lists them in project-values.md).
    [string]$KnownMalformedIds = ''
)

# Ledger validator.
# ERROR (script fails): duplicate ID, malformed row not on the known list, no rows found.
# WARN  (printed, script passes): legacy content problems the ledger already had when this check was repaired
#       (bad date cell, unknown status word, terminal state without resolution, missing evidence, bad duplicate target).
$ErrorActionPreference = 'Stop'
$allowedTypes = @('bug', 'risk', 'gap', 'planned')
$allowedSeverities = @('critical', 'high', 'medium', 'low', 'unknown')
$allowedStatuses = @(
    'SUSPECTED', 'CONFIRMED', 'IN_PROGRESS', 'VERIFY_PENDING', 'RESOLVED',
    'REJECTED', 'DEFERRED', 'WONT_FIX', 'DUPLICATE'
)
$terminalWithReason = @('RESOLVED', 'REJECTED', 'WONT_FIX', 'DUPLICATE')
# Old IDs are BUG-NNNN (frozen); new IDs are BUG-YYMMDD-xxxx (4 lowercase hex).
$idPattern = 'BUG-(\d{4}|\d{6}-[0-9a-f]{4})'
# Rows that were already structurally broken when this check was repaired (2026-10). Remove an ID once its row is fixed.
$knownMalformed = @($KnownMalformedIds -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })

if (-not (Test-Path -LiteralPath $LedgerPath)) { throw "Ledger not found: $LedgerPath" }
$content = Get-Content -LiteralPath $LedgerPath

# Split a row on '|' that is neither backslash-escaped nor inside a `code span`.
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

# Only rows under "## Ledger": the renumbering logs further down repeat old IDs on purpose.
$inLedger = $false
$rows = @(foreach ($line in $content) {
    if ($line -match '^## Ledger\s*$') { $inLedger = $true; continue }
    if ($inLedger -and $line -match '^## ') { break }
    if ($inLedger -and $line -match "^\| $idPattern \|") { $line }
})
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
    # Status cell is wrapped in a colour span: <span class="st-resolved">RESOLVED</span>
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
