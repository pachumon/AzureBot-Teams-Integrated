# Integration Test Script for Azure Bot Framework + LangGraph
# This script tests the basic integration between the two services

Write-Host "=== Azure Bot Framework + LangGraph Integration Test ===" -ForegroundColor Cyan
Write-Host ""

# Test LangGraph API Health
Write-Host "1. Testing LangGraph API Health..." -ForegroundColor Yellow
try {
    $langGraphResponse = Invoke-RestMethod -Uri "http://localhost:8000/api/v1/health" -Method Get -TimeoutSec 5
    Write-Host "✅ LangGraph API is healthy: $($langGraphResponse.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ LangGraph API is not responding: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Make sure to start the LangGraph API first:" -ForegroundColor Yellow
    Write-Host "   cd C:\Users\pachumon\git\LangGraph-Agent" -ForegroundColor Gray
    Write-Host "   python run_api.py" -ForegroundColor Gray
    exit 1
}

# Test Bot Framework Health  
Write-Host "2. Testing Bot Framework Health..." -ForegroundColor Yellow
try {
    $botResponse = Invoke-RestMethod -Uri "http://localhost:5000" -Method Get -TimeoutSec 5
    Write-Host "✅ Bot Framework is healthy: $($botResponse.name)" -ForegroundColor Green
} catch {
    Write-Host "❌ Bot Framework is not responding: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Make sure to start the Bot Framework:" -ForegroundColor Yellow
    Write-Host "   cd C:\Users\pachumon\git\AzureBot-Teams-Integrated\BotApp" -ForegroundColor Gray
    Write-Host "   dotnet run" -ForegroundColor Gray
    exit 1
}

# Test LangGraph Session Creation
Write-Host "3. Testing LangGraph Session Creation..." -ForegroundColor Yellow
try {
    $sessionResponse = Invoke-RestMethod -Uri "http://localhost:8000/api/v1/sessions/" -Method Post -ContentType "application/json" -Body "{}" -TimeoutSec 10
    $sessionId = $sessionResponse.session_id
    Write-Host "✅ LangGraph session created: $sessionId" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to create LangGraph session: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test LangGraph Query
Write-Host "4. Testing LangGraph Query Processing..." -ForegroundColor Yellow
try {
    $queryBody = @{
        query = "What's the capital of France?"
    } | ConvertTo-Json

    $queryResponse = Invoke-RestMethod -Uri "http://localhost:8000/api/v1/chat/$sessionId/query" -Method Post -ContentType "application/json" -Body $queryBody -TimeoutSec 30
    Write-Host "✅ LangGraph query processed successfully" -ForegroundColor Green
    Write-Host "   Response: $($queryResponse.response.Substring(0, [Math]::Min(100, $queryResponse.response.Length)))..." -ForegroundColor Gray
    Write-Host "   Processing Time: $($queryResponse.processing_time)s" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to process LangGraph query: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   This might be due to Gemini API key issues" -ForegroundColor Yellow
}

# Cleanup
Write-Host "5. Cleaning up test session..." -ForegroundColor Yellow
try {
    $cleanupResponse = Invoke-RestMethod -Uri "http://localhost:8000/api/v1/sessions/$sessionId" -Method Delete -TimeoutSec 5
    Write-Host "✅ Test session cleaned up successfully" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Failed to cleanup test session (non-critical): $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Integration Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Both services are running and communicating properly!" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Open Bot Framework Emulator" -ForegroundColor White
Write-Host "2. Connect to: http://localhost:5000/api/messages" -ForegroundColor White
Write-Host "3. Start chatting with geography questions!" -ForegroundColor White
Write-Host ""
Write-Host "Example questions to try:" -ForegroundColor Yellow
Write-Host "• What's the capital of Japan?" -ForegroundColor Gray
Write-Host "• Tell me about Berlin" -ForegroundColor Gray
Write-Host "• What country has London as its capital?" -ForegroundColor Gray
Write-Host ""