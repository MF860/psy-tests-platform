# Arabic Text Sanitization - Global Replacement Script
# Wraps ALL Arabic text with DesignTokens.ArText.Sanitize()

$serviceFile = "backend\PsyApi\Services\Reports\UltraHiFiPdfReportService.cs"

Write-Host "🔧 Applying Arabic sanitization to $serviceFile..." -ForegroundColor Cyan

# Read file content
$content = Get-Content -Path $serviceFile -Raw

# Add helper methods after Helper Methods region
$helperMethodsInsert = @"
        #region Arabic Text Sanitization Helpers

        /// <summary>
        /// Get localized string with automatic Arabic sanitization
        /// ALWAYS use this instead of LocalizationStrings.Get() directly
        /// </summary>
        private static string L(string key)
        {
            var raw = LocalizationStrings.Get(key);
            return DesignTokens.ArText.Sanitize(raw);
        }

        /// <summary>
        /// Sanitize any Arabic text (dimension names, user input, etc.)
        /// </summary>
        private static string Ar(string? text)
        {
            return DesignTokens.ArText.Sanitize(text);
        }

        #endregion

        #region Helper Methods
"@

$content = $content -replace '#region Helper Methods', $helperMethodsInsert

# Replace LocalizationStrings.Get with L() wrapper
$content = $content -replace 'LocalizationStrings\.Get\(', 'L('

# Replace DesignTokens.ArabicNormalization.NormalizeArabic with Ar
$content = $content -replace 'DesignTokens\.ArabicNormalization\.NormalizeArabic\(', 'Ar('

# Replace dimension names with Ar() wrapper
$content = $content -replace '(\w+)\.Dimension(?!\s*==)', 'Ar($1.Dimension)'
$content = $content -replace 'Ar\(Ar\(', 'Ar(' # Remove double wrapping

# Replace action titles with Ar() wrapper
$content = $content -replace 'action\.Title(?!\s*==)', 'Ar(action.Title)'

# Replace user.FullName with Ar() wrapper
$content = $content -replace 'user\.FullName\s*\?\?', 'Ar(user.FullName) ??'

# Save modified content
$content | Set-Content -Path $serviceFile -NoNewline

Write-Host "✅ Arabic sanitization applied successfully!" -ForegroundColor Green
Write-Host "📝 Modified: $serviceFile" -ForegroundColor Yellow
