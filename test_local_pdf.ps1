# ============================================================================
# Quick Local PDF Test - Localhost
# ============================================================================
# Tests local development Ultra Hi-Fi PDF generation
# Uses existing seeded user (1000000001) for faster testing
# ============================================================================

.\test_sdj_session_and_pdf.ps1 `
    -BaseUrl "http://localhost:5124" `
    -AdminUsername "root" `
    -AdminPassword "StrongAdmin!23!" `
    -TestUserNationalId "1000000001"
