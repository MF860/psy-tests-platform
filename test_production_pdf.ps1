# ============================================================================
# Quick Production PDF Test - Render + Vercel
# ============================================================================
# Tests the LIVE production Ultra Hi-Fi PDF generation
# Uses existing seeded user (1000000001) for faster testing
# ============================================================================

.\test_sdj_session_and_pdf.ps1 `
    -BaseUrl "https://psy-api-backend.onrender.com" `
    -AdminUsername "root" `
    -AdminPassword "StrongAdmin!23!" `
    -TestUserNationalId "1000000001"
