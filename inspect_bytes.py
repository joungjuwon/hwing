
import os

files = [
    r'C:\Users\User\Documents\GitHub\hwing\Assets\_project\Shaders\Watercolor\Objects\Watercolour.shader',
    r'C:\Users\User\Documents\GitHub\hwing\Assets\_project\Shaders\Watercolor\Objects\WatercolourLeaves.shader'
]

for fpath in files:
    print(f"--- Checking {os.path.basename(fpath)} ---")
    try:
        with open(fpath, 'rb') as f:
            lines = f.readlines()
            # Check lines 1 to 20
            for i in range(0, min(20, len(lines))):
                line_bytes = lines[i]
                
                suspicious = []
                for b in line_bytes:
                    # Allow tab(9), newline(10), carriage return(13), and printable ASCII (32-126)
                    if not (32 <= b <= 126 or b in [9, 10, 13]):
                        suspicious.append(b)
                
                if suspicious:
                    print(f"Line {i+1}: {line_bytes}")
                    print(f"  [!] SUSPICIOUS BYTES: {suspicious}")
                else:
                    # print(f"Line {i+1}: OK") 
                    pass
            print(f"Finished checking first 20 lines of {os.path.basename(fpath)}")
                    
    except Exception as e:
        print(f"Error reading {fpath}: {e}")
