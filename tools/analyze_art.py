with open(r'e:\Cathedral\assets\art\humors2\ascii_art.txt', 'r', encoding='utf-8') as f:
    art_lines = [line.rstrip('\n') for line in f]
with open(r'e:\Cathedral\assets\art\humors2\layer_map.txt', 'r', encoding='utf-8') as f:
    map_lines = [line.rstrip('\n') for line in f]

print(f'Art lines: {len(art_lines)}, Map lines: {len(map_lines)}')
print(f'Map line width: {len(map_lines[0])}')
print()

# Box drawing chars - letters adjacent to these are inner labels, not border organ letters
box_chars = set('║═╔╗╚╝╠╣╦╩╬')

print('All capital ASCII letters in art file:')
for li, line in enumerate(art_lines):
    chars = list(line)
    for ci, ch in enumerate(chars):
        if ch.isupper() and ch.isascii() and ch.isalpha():
            l1 = chars[ci-1] if ci > 0 else ' '
            l2 = chars[ci-2] if ci > 1 else ' '
            r1 = chars[ci+1] if ci < len(chars)-1 else ' '
            r2 = chars[ci+2] if ci < len(chars)-2 else ' '
            adj = l1 in box_chars or r1 in box_chars or l2 in box_chars or r2 in box_chars
            ctx = ''.join(chars[max(0,ci-4):ci+5])
            map_val = map_lines[li][ci] if li < len(map_lines) and ci < len(map_lines[li]) else '?'
            print(f'  L{li+1:2d} C{ci:2d}(0-idx): [{ch}] ctx="{ctx}" box_adj={adj} map={map_val}')
