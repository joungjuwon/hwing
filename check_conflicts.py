import subprocess
import os

def get_conflicted_files():
    try:
        # Get list of unmerged files
        result = subprocess.run(['git', 'diff', '--name-only', '--diff-filter=U'], 
                              capture_output=True, text=True, encoding='utf-8')
        return [f for f in result.stdout.split('\n') if f.strip()]
    except Exception as e:
        return []

def get_file_date(branch, filename):
    try:
        # Get commit date for the file in the specified branch
        result = subprocess.run(['git', 'log', '-1', '--format=%cd', branch, '--', filename],
                              capture_output=True, text=True, encoding='utf-8')
        return result.stdout.strip()
    except:
        return "Unknown"

def main():
    files = get_conflicted_files()
    if not files:
        print("No conflicted files found or error reading list.")
        return

    with open('conflict_report.txt', 'w', encoding='utf-8') as f:
        for i, file in enumerate(files):
            date_ours = get_file_date('HEAD', file)
            date_theirs = get_file_date('MERGE_HEAD', file)
            f.write(f"{i+1}. {file}\n")
            f.write(f"   - Ours (HEAD):       {date_ours}\n")
            f.write(f"   - Theirs (Incoming): {date_theirs}\n")
            f.write("\n")
    print("Report written to conflict_report.txt")

if __name__ == "__main__":
    main()
