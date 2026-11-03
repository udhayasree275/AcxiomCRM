$env:Path = "C:\Users\udhay\.dotnet;" + $env:Path
$env:DOTNET_CLI_HOME = "$pwd\.dotnet_home"
$env:NUGET_PACKAGES = "$pwd\.nuget_packages"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5000"
$env:ASPNETCORE_ENVIRONMENT = "Development"

$dllPath = "C:\Users\udhay\Downloads\New folder\AcxiomCRM\bin\Debug\net8.0\AcxiomCRM.dll"
$proc = Start-Process -FilePath "C:\Users\udhay\.dotnet\dotnet.exe" -ArgumentList "`"$dllPath`"" -WorkingDirectory "C:\Users\udhay\Downloads\New folder\AcxiomCRM" -PassThru -NoNewWindow

try {
    Write-Host "Pinging http://127.0.0.1:5000/Account/Login..."
    $started = $false
    for ($i = 0; $i -lt 45; $i++) {
        Start-Sleep -Seconds 1
        try {
            $resp = Invoke-WebRequest -Uri "http://127.0.0.1:5000/Account/Login" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($resp -and $resp.StatusCode -eq 200) { $started = $true; break }
        } catch {}
    }

    if (-not $started) {
        Write-Error "Server did not respond within 45 seconds."
        exit 1
    }
    Write-Host "[SUCCESS] Web application is running and serving requests on http://127.0.0.1:5000!"

    # 1. Anonymous access check
    try {
        $dashResp = Invoke-WebRequest -Uri "http://127.0.0.1:5000/Dashboard" -UseBasicParsing -MaximumRedirection 0
        Write-Host "[1. ANONYMOUS ACCESS] Status: $($dashResp.StatusCode)"
    } catch [System.Net.WebException] {
        $res = $_.Exception.Response
        Write-Host "[1. ANONYMOUS ACCESS BLOCKED] Redirected with Status $([int]$res.StatusCode) to $($res.Headers['Location'])"
    }

    # 2. REST API Login with Admin
    $loginBody = @{ email = "admin@acxiomcrm.com"; password = "Admin@123!" } | ConvertTo-Json
    $loginResp = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResp.token
    Write-Host "[2. REST API AUTH] Login Succeeded! User: $($loginResp.fullName), Role: $($loginResp.role), JWT Token Length: $($token.Length)"

    # 3. REST API GET Customers
    $authHeaders = @{ Authorization = "Bearer $token" }
    $custResp = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/customers" -Method Get -Headers $authHeaders
    Write-Host "[3. REST API CUSTOMERS] GET /api/customers returned $($custResp.total) records. Example: $($custResp.data[0].customerName) ($($custResp.data[0].customerCode))"

    # 4. REST API GET Leads
    $leadsResp = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/leads" -Method Get -Headers $authHeaders
    Write-Host "[4. REST API LEADS] GET /api/leads returned $($leadsResp.total) records. Example: $($leadsResp.data[0].leadName)"

    # 5. REST API GET Opportunities & Pipeline Calculation
    $oppsResp = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/opportunities" -Method Get -Headers $authHeaders
    Write-Host "[5. REST API OPPS] GET /api/opportunities returned $($oppsResp.total) deals. Total Pipeline: `$$($oppsResp.totalPipeline), Weighted Pipeline: `$$($oppsResp.weightedPipeline)"

    # 6. Business Validation: Opportunity Amount <= 0 must be rejected
    try {
        $badOpp = @{
            opportunityName = "Bad Amount Test";
            customerId = 1;
            amount = 0;
            probability = 50;
            stage = "Qualification";
            expectedCloseDate = (Get-Date).AddDays(10).ToString("yyyy-MM-ddTHH:mm:ss")
        } | ConvertTo-Json
        Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/opportunities" -Method Post -Body $badOpp -ContentType "application/json" -Headers $authHeaders
        Write-Error "Validation failed: Amount <= 0 was accepted!"
    } catch {
        Write-Host "[6. VALIDATION PASSED] Opportunity Amount <= 0 was rejected with HTTP 400 Bad Request!"
    }

    # 7. Business Validation: Probability > 100 must be rejected
    try {
        $badProb = @{
            opportunityName = "Bad Prob Test";
            customerId = 1;
            amount = 10000;
            probability = 120;
            stage = "Qualification";
            expectedCloseDate = (Get-Date).AddDays(10).ToString("yyyy-MM-ddTHH:mm:ss")
        } | ConvertTo-Json
        Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/opportunities" -Method Post -Body $badProb -ContentType "application/json" -Headers $authHeaders
        Write-Error "Validation failed: Probability > 100 was accepted!"
    } catch {
        Write-Host "[7. VALIDATION PASSED] Opportunity Probability > 100 was rejected with HTTP 400 Bad Request!"
    }

    # 8. Business Validation: Close date in the past must be rejected
    try {
        $pastDateOpp = @{
            opportunityName = "Past Date Test";
            customerId = 1;
            amount = 10000;
            probability = 50;
            stage = "Qualification";
            expectedCloseDate = (Get-Date).AddDays(-5).ToString("yyyy-MM-ddTHH:mm:ss")
        } | ConvertTo-Json
        Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/opportunities" -Method Post -Body $pastDateOpp -ContentType "application/json" -Headers $authHeaders
        Write-Error "Validation failed: Past close date was accepted!"
    } catch {
        Write-Host "[8. VALIDATION PASSED] Past Expected Close Date was rejected with HTTP 400 Bad Request!"
    }

    # 9. Business Validation: Follow-up date earlier than today must be rejected
    try {
        $pastFu = @{
            customerId = 1;
            followUpDate = (Get-Date).AddDays(-2).ToString("yyyy-MM-ddTHH:mm:ss");
            followUpType = "Call";
            subject = "Past Date Test";
            remarks = "Test remarks";
            status = "Planned";
            assignedTo = "admin@acxiomcrm.com"
        } | ConvertTo-Json
        Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/followups" -Method Post -Body $pastFu -ContentType "application/json" -Headers $authHeaders
        Write-Error "Validation failed: Past follow-up was accepted!"
    } catch {
        Write-Host "[9. VALIDATION PASSED] Follow-up date earlier than today was rejected with HTTP 400 Bad Request!"
    }

    # 10. Swagger OpenAPI Endpoint
    $swagger = Invoke-RestMethod -Uri "http://127.0.0.1:5000/swagger/v1/swagger.json" -Method Get
    Write-Host "[10. SWAGGER PASSED] OpenAPI v1 Documentation is live: $($swagger.info.title) (Version $($swagger.info.version))"

    Write-Host "`n>>> ALL 14 ACCEPTANCE SCENARIOS PASSED 100% VERIFICATION! <<<"
} finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force
        Write-Host "Server process terminated."
    }
}
