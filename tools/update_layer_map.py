with open(r'e:\Cathedral\assets\art\humors2\ascii_art.txt', 'r', encoding='utf-8') as f:
    art_lines = [line.rstrip('\n') for line in f]
with open(r'e:\Cathedral\assets\art\humors2\layer_map.txt', 'r', encoding='utf-8') as f:
    raw_lines = f.read().splitlines()

# Work with list-of-lists for easy mutation
map_chars = [list(line) for line in raw_lines]

box_chars = set('║═╔╗╚╝╠╣╦╩╬')

updated = []
for li, line in enumerate(art_lines):
    chars = list(line)
    for ci, ch in enumerate(chars):
        if ch.isupper() and ch.isascii() and ch.isalpha():
            l1 = chars[ci-1] if ci > 0 else ' '
            r1 = chars[ci+1] if ci < len(chars)-1 else ' '
            l2 = chars[ci-2] if ci > 1 else ' '
            r2 = chars[ci+2] if ci < len(chars)-2 else ' '
            adj = l1 in box_chars or r1 in box_chars or l2 in box_chars or r2 in box_chars
            if not adj:
                if li < len(map_chars) and ci < len(map_chars[li]):
                    old = map_chars[li][ci]
                    map_chars[li][ci] = '3'
                    updated.append((li+1, ci, ch, old))

print("Updated positions:")
for row, col, ch, old in updated:
    print(f"  L{row:2d} C{col:2d}: [{ch}] {old} -> 3")

# Write back preserving original line endings
with open(r'e:\Cathedral\assets\art\humors2\layer_map.txt', 'w', encoding='utf-8', newline='\n') as f:
    for line in map_chars:
        f.write(''.join(line) + '\n')

print(f"\nDone. {len(updated)} positions updated to '3'.")
