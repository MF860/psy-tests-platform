-- Create a completed SDJ session and result with sdjData for testing AI analyzer

-- First, verify we have user with ID 2 (1000000002)
-- Session record with completed status and sdjData in Payload
INSERT INTO Sessions (UserId, SessionId, Status, StartedAt, EndedAt, LastActivityAt, CurrentIndex, Payload)
VALUES (
    2,
    'mock-sdj-test-session-001',
    'completed',
    datetime('now', '-1 hour'),
    datetime('now', '-5 minutes'),
    datetime('now', '-5 minutes'),
    120,
    '{"sdjData":{"Dimensions":[{"Dimension":"الواقعية","Raw":28.5,"T":55.2,"Percentile":69.1,"Band":"متوسط"},{"Dimension":"الاستقصائية","Raw":32.1,"T":58.7,"Percentile":79.5,"Band":"مرتفع"},{"Dimension":"الفنية","Raw":18.3,"T":45.8,"Percentile":35.2,"Band":"منخفض"},{"Dimension":"الاجتماعية","Raw":35.6,"T":62.4,"Percentile":87.3,"Band":"مرتفع جداً"},{"Dimension":"المغامرة","Raw":25.2,"T":52.1,"Percentile":58.7,"Band":"متوسط"}],"SubDimensions":[{"Dimension":"الواقعية","SubDimension":"العمل اليدوي","T":56.3,"Band":"متوسط"},{"Dimension":"الواقعية","SubDimension":"الميكانيكية","T":54.1,"Band":"متوسط"},{"Dimension":"الاستقصائية","SubDimension":"التحليل","T":61.2,"Band":"مرتفع"},{"Dimension":"الاستقصائية","SubDimension":"البحث العلمي","T":56.4,"Band":"متوسط"},{"Dimension":"الفنية","SubDimension":"الإبداع","T":47.2,"Band":"متوسط"},{"Dimension":"الفنية","SubDimension":"التصميم","T":44.5,"Band":"منخفض"},{"Dimension":"الاجتماعية","SubDimension":"التواصل","T":65.8,"Band":"مرتفع جداً"},{"Dimension":"الاجتماعية","SubDimension":"المساعدة","T":59.1,"Band":"مرتفع"},{"Dimension":"المغامرة","SubDimension":"القيادة","T":53.7,"Band":"متوسط"},{"Dimension":"المغامرة","SubDimension":"الإقناع","T":50.6,"Band":"متوسط"}],"TrackFits":[{"TrackNameAr":"المسار الاجتماعي","FitLevel":"ملاءمة عالية","FitScore":87.3},{"TrackNameAr":"المسار الاستقصائي","FitLevel":"ملاءمة جيدة","FitScore":79.5},{"TrackNameAr":"المسار المغامر","FitLevel":"ملاءمة متوسطة","FitScore":58.7}]}}'
);

-- Get the session ID we just created
-- Now create a Result record with proper dimension scores JSON
INSERT INTO Results (SessionId, TotalScore, DimensionScoresJson, CompositeScoresJson, ScoringModelVersion, CreatedAt)
VALUES (
    (SELECT Id FROM Sessions WHERE SessionId = 'mock-sdj-test-session-001'),
    489,
    '{"SubDimensions":[{"Dimension":"الواقعية","SubDimension":"العمل اليدوي","Raw":14.2,"T":56.3,"Percentile":69.1,"Band":"متوسط"},{"Dimension":"الواقعية","SubDimension":"الميكانيكية","Raw":14.3,"T":54.1,"Percentile":65.8,"Band":"متوسط"},{"Dimension":"الاستقصائية","SubDimension":"التحليل","Raw":16.5,"T":61.2,"Percentile":82.4,"Band":"مرتفع"},{"Dimension":"الاستقصائية","SubDimension":"البحث العلمي","Raw":15.6,"T":56.4,"Percentile":71.2,"Band":"متوسط"},{"Dimension":"الفنية","SubDimension":"الإبداع","Raw":9.8,"T":47.2,"Percentile":42.1,"Band":"متوسط"},{"Dimension":"الفنية","SubDimension":"التصميم","Raw":8.5,"T":44.5,"Percentile":31.5,"Band":"منخفض"},{"Dimension":"الاجتماعية","SubDimension":"التواصل","Raw":18.2,"T":65.8,"Percentile":91.2,"Band":"مرتفع جداً"},{"Dimension":"الاجتماعية","SubDimension":"المساعدة","Raw":17.4,"T":59.1,"Percentile":78.5,"Band":"مرتفع"},{"Dimension":"المغامرة","SubDimension":"القيادة","Raw":13.1,"T":53.7,"Percentile":61.2,"Band":"متوسط"},{"Dimension":"المغامرة","SubDimension":"الإقناع","Raw":12.1,"T":50.6,"Percentile":54.3,"Band":"متوسط"}],"Dimensions":[{"Dimension":"الواقعية","Raw":28.5,"T":55.2,"Percentile":69.1,"Band":"متوسط"},{"Dimension":"الاستقصائية","Raw":32.1,"T":58.7,"Percentile":79.5,"Band":"مرتفع"},{"Dimension":"الفنية","Raw":18.3,"T":45.8,"Percentile":35.2,"Band":"منخفض"},{"Dimension":"الاجتماعية","Raw":35.6,"T":62.4,"Percentile":87.3,"Band":"مرتفع جداً"},{"Dimension":"المغامرة","Raw":25.2,"T":52.1,"Percentile":58.7,"Band":"متوسط"}]}',
    '{"type":"SDJ","riasecCode":"SI","topTracksFit":[{"track":"الاجتماعي","score":87.3},{"track":"الاستقصائي","score":79.5}]}',
    'SDJ-v1',
    datetime('now', '-5 minutes')
);

-- Verify the data was inserted
SELECT 'Session Created:' as status, SessionId, Status, UserId FROM Sessions WHERE SessionId = 'mock-sdj-test-session-001';
SELECT 'Result Created:' as status, r.Id as ResultId, r.TotalScore, s.SessionId FROM Results r JOIN Sessions s ON r.SessionId = s.Id WHERE s.SessionId = 'mock-sdj-test-session-001';
