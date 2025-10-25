#!/usr/bin/env python3
"""
Transform legacy question bank to SDJ-aligned Likert items.
Converts all TEXT items to LikertAgreement with SDJ dimensions.
"""

import csv
import re
from typing import Dict, List, Tuple

# SDJ Dimension Mapping (Legacy dimension_tags → SDJ parent & sub)
SDJ_DIMENSION_MAP = {
    # Self-Excellence (التميز الذاتي)
    'الاهتمامات الشخصية': ('التميز الذاتي', 'الوعي الذاتي'),
    'التعبير الذاتي': ('التميز الذاتي', 'الوعي الذاتي'),
    'الوعي الذاتي': ('التميز الذاتي', 'الوعي الذاتي'),
    'الثقة بالنفس': ('التميز الذاتي', 'الثقة بالنفس'),
    'التنظيم': ('التميز الذاتي', 'التنظيم الذاتي'),
    'التعلم الذاتي': ('التميز الذاتي', 'التعلم المستمر'),
    'التطوير الشخصي': ('التميز الذاتي', 'التعلم المستمر'),
    'المرونة': ('التميز الذاتي', 'المرونة النفسية'),
    'الطموح': ('التميز الذاتي', 'التنظيم الذاتي'),
    'الانتباه': ('التميز الذاتي', 'التنظيم الذاتي'),
    
    # Communication & Relationships (التواصل والعلاقات)
    'الذكاء العاطفي': ('التواصل والعلاقات', 'الذكاء العاطفي'),
    'التواصل': ('التواصل والعلاقات', 'التواصل الفعال'),
    'التعاون': ('التواصل والعلاقات', 'التعاون'),
    'حل المشكلات': ('النجاح المهني', 'حل المشكلات'),  # Overlaps with Career
    
    # Career Success (النجاح المهني)
    'القيادة': ('النجاح المهني', 'القيادة'),
    'الإبداع': ('النجاح المهني', 'الإبداع والابتكار'),
    'التخطيط': ('النجاح المهني', 'التخطيط الاستراتيجي'),
    'الدافعية': ('النجاح المهني', 'الإبداع والابتكار'),
    'المسؤولية': ('النجاح المهني', 'القيادة'),
    'الرضا الوظيفي': ('النجاح المهني', 'التخطيط الاستراتيجي'),
    
    # Social Responsibility (المسؤولية الاجتماعية)
    'الأخلاق': ('المسؤولية الاجتماعية', 'الأخلاق المهنية'),
    'المسؤولية الاجتماعية': ('المسؤولية الاجتماعية', 'الوعي المجتمعي'),
    
    # Health & Balance (الصحة والتوازن)
    'الصحة النفسية': ('الصحة والتوازن', 'الصحة النفسية'),
    'الصحة الجسدية': ('الصحة والتوازن', 'الصحة الجسدية'),
    
    # Knowledge dimensions (keep as MCQ/ORDERING/NUMERIC - not converted)
    'المعرفة العامة': None,
    'المعرفة الجغرافية': None,
    'المعرفة التاريخية': None,
    'المعرفة العلمية': None,
    'المعرفة النفسية': None,
    'المعرفة التكنولوجية': None,
    'المعرفة البيولوجية': None,
    'التفكير النقدي': ('التميز الذاتي', 'التعلم المستمر'),
    'الحساب الذهني': None,  # Keep as TIMED_NUMERIC
    'اللغة': None,  # Keep as ORDERING
}

# Standard Likert anchors
LIKERT_ANCHORS = "لا أوافق بشدة|لا أوافق|محايد|أوافق|أوافق بشدة"

def text_to_likert_statement(text: str, dimension_tags: str) -> Tuple[str, int]:
    """
    Convert open-ended TEXT prompt to Likert agreement statement.
    Returns (statement, reverse_flag)
    """
    text = text.strip()
    
    # Remove question marks and prompts
    text = re.sub(r'[؟?]+$', '', text)
    text = re.sub(r'^(ما هي|كيف|صف|اشرح|كم مرة)\s+', '', text, flags=re.IGNORECASE)
    
    # Patterns that convert to Likert statements
    conversions = [
        # "ما هي X؟" → "أفهم مفهوم X"
        (r'مفهوم (.+)', r'أفهم مفهوم \1 بوضوح.'),
        # "صف X" → "أستطيع وصف X"
        (r'(.+) في ثلاث كلمات', r'أستطيع وصف \1 بوضوح.'),
        # "كيف تتعامل مع X" → "أتعامل مع X بفعالية"
        (r'كيف تتعامل مع (.+)', r'أتعامل مع \1 بفعالية.'),
        # "اشرح X" → "أستطيع شرح X"
        (r'(.+)', r'لدي فهم واضح لـ \1.'),
    ]
    
    statement = text
    for pattern, replacement in conversions:
        match = re.search(pattern, text)
        if match:
            statement = re.sub(pattern, replacement, text)
            break
    
    # Ensure statement ends with period
    if not statement.endswith('.'):
        statement += '.'
    
    # Determine if reverse scoring needed (negatively worded)
    reverse = 0
    negative_markers = ['لا', 'صعب', 'مشكلة', 'ضغط', 'فشل', 'نقد']
    if any(marker in text for marker in negative_markers):
        reverse = 1
    
    return statement, reverse


def transform_csv(input_path: str, output_path: str):
    """Transform legacy CSV to SDJ format."""
    
    with open(input_path, 'r', encoding='utf-8') as infile:
        reader = csv.DictReader(infile)
        rows = list(reader)
    
    sdj_rows = []
    text_count = 0
    converted_count = 0
    skipped_count = 0
    
    for row in rows:
        item_id = row['item_id']
        text_ar = row['text_ar']
        item_type = row['type']
        dimension_tags = row['dimension_tags']
        difficulty = row['difficulty'] or '2'
        time_limit = row['time_limit_seconds'] or '45'
        max_score = row['max_score'] or '5'
        correct_answer = row['correct_answer']
        options = row['options']
        
        # Get SDJ mapping
        sdj_mapping = SDJ_DIMENSION_MAP.get(dimension_tags)
        
        # Convert TEXT to LikertAgreement if SDJ mapping exists
        if item_type == 'Text':
            text_count += 1
            if sdj_mapping:
                likert_statement, reverse = text_to_likert_statement(text_ar, dimension_tags)
                sdj_rows.append({
                    'item_code': item_id,
                    'text_ar': likert_statement,
                    'type': 'LikertAgreement',
                    'dimension': sdj_mapping[0],
                    'sub_dimension': sdj_mapping[1],
                    'anchors_ar': LIKERT_ANCHORS,
                    'reverse': reverse,
                    'time_limit_seconds': '45',
                    'max_score': '5',
                    'difficulty': difficulty
                })
                converted_count += 1
            else:
                # Skip TEXT items without SDJ mapping (knowledge questions)
                skipped_count += 1
                print(f"Skipped TEXT item {item_id}: {dimension_tags} (no SDJ mapping)")
        
        # Keep non-TEXT items if they have SDJ mapping or are knowledge tests
        elif item_type in ['LikertAgreement', 'Frequency']:
            if sdj_mapping:
                sdj_rows.append({
                    'item_code': item_id,
                    'text_ar': text_ar,
                    'type': item_type,
                    'dimension': sdj_mapping[0],
                    'sub_dimension': sdj_mapping[1],
                    'anchors_ar': options or LIKERT_ANCHORS,
                    'reverse': '0',
                    'time_limit_seconds': time_limit,
                    'max_score': max_score,
                    'difficulty': difficulty
                })
            else:
                skipped_count += 1
        
        # Keep MCQ, ORDERING, TIMED_NUMERIC as-is (not converted to SDJ)
        # These are knowledge/skill tests, not personality dimensions
        # They will be filtered out in the final SDJ CSV
    
    # Write SDJ CSV
    if sdj_rows:
        with open(output_path, 'w', encoding='utf-8', newline='') as outfile:
            fieldnames = ['item_code', 'text_ar', 'type', 'dimension', 'sub_dimension', 
                         'anchors_ar', 'reverse', 'time_limit_seconds', 'max_score', 'difficulty']
            writer = csv.DictWriter(outfile, fieldnames=fieldnames)
            writer.writeheader()
            writer.writerows(sdj_rows)
    
    print(f"\n=== Transformation Summary ===")
    print(f"Total rows processed: {len(rows)}")
    print(f"TEXT items found: {text_count}")
    print(f"TEXT → Likert converted: {converted_count}")
    print(f"Skipped (no SDJ mapping): {skipped_count}")
    print(f"Total SDJ items: {len(sdj_rows)}")
    print(f"\nOutput: {output_path}")
    
    # Dimension distribution
    dim_counts = {}
    for row in sdj_rows:
        dim = row['dimension']
        dim_counts[dim] = dim_counts.get(dim, 0) + 1
    
    print(f"\n=== SDJ Dimension Distribution ===")
    for dim, count in sorted(dim_counts.items(), key=lambda x: -x[1]):
        print(f"{dim}: {count} items")


if __name__ == '__main__':
    import sys
    
    input_csv = 'seed/questions_fixed_extended_plus_personality.csv'
    output_csv = 'seed/questions_sdj_ar.csv'
    
    if len(sys.argv) > 1:
        input_csv = sys.argv[1]
    if len(sys.argv) > 2:
        output_csv = sys.argv[2]
    
    print(f"Transforming {input_csv} → {output_csv}...")
    transform_csv(input_csv, output_csv)
    print("\n✅ Transformation complete!")
