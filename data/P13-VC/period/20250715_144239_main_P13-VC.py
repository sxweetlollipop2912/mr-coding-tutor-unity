# User P13-VC main code file  
n = 153
x = str(n)
kq = 0
y =0
while n>0:
    y = n%10
    print(y)
    n = n//10
    kq += y**len(x)
if kq == n:
    print('YES')
else:
    print("NO")