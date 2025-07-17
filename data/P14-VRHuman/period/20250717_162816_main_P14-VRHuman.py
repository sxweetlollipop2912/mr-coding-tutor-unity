# User P14-VRHuman main code file  
x = 153
s = str(x)
n = len(s)
val = 0

for i in range(0, 1):
    c = s[i]
    d = int(c)
    e = d**n
    val = val+e

print(val)
if val==x:
    print('Yes')
else:
    print('n')