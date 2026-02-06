import subprocess
import datetime
import os

CUTOFF_DATE = datetime.datetime(2026, 2, 1, tzinfo=datetime.timezone(datetime.timedelta(hours=9))) # KST approximation or assume local consistency

def get_conflicted_files():
    try:
        result = subprocess.run(['git', 'diff', '--name-only', '--diff-filter=U'], 
                              capture_output=True, text=True, encoding='utf-8')
        return [f.strip() for f in result.stdout.split('\n') if f.strip()]
    except:
        return []

def get_file_timestamp(branch, filename):
    try:
        # Use ISO format for easy parsing: 2026-01-29T15:11:35+09:00
        result = subprocess.run(['git', 'log', '-1', '--format=%cI', branch, '--', filename],
                              capture_output=True, text=True, encoding='utf-8')
        out = result.stdout.strip()
        if not out: return None
        return datetime.datetime.fromisoformat(out)
    except:
        return None

def main():
    files = get_conflicted_files()
    skipped_files = []
    
    with open('resolution_log.txt', 'w', encoding='utf-8') as log:
        log.write(f"Resolution Report (Cutoff: {CUTOFF_DATE})\n")
        log.write("-" * 50 + "\n")

        for file in files:
            ours_date = get_file_timestamp('HEAD', file)
            
            should_resolve = False
            reason = ""
            
            if ours_date is None:
                should_resolve = True
                reason = "Not present in our branch (New file)"
            elif ours_date < CUTOFF_DATE:
                should_resolve = True
                reason = f"Older than cutoff ({ours_date})"
            else:
                should_resolve = False
                reason = f"Modified recently ({ours_date})"

            if should_resolve:
                print(f"Resolving {file} using THEIRS ({reason})")
                log.write(f"[RESOLVED] {file}: {reason}\n")
                subprocess.run(['git', 'checkout', '--theirs', file])
                subprocess.run(['git', 'add', file])
            else:
                print(f"Skipping {file} ({reason})")
                log.write(f"[SKIPPED]  {file}: {reason}\n")
                skipped_files.append(file)

    if skipped_files:
        print("\nSkipped files (Require manual attention):")
        for f in skipped_files:
            print(f"- {f}")
    else:
        print("\nAll conflicts auto-resolved according to criteria.")

if __name__ == "__main__":
    main()
