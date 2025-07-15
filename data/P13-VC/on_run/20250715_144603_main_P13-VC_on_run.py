# User P13-VC main code file  
n = 1253
bien = n
x = str(n)
kq = 0
y =0
while n>0:
    y = n%10
    n = n//10
    kq += y**len(x)
if kq == bien:
    print('YES')
else:
    print("NO")