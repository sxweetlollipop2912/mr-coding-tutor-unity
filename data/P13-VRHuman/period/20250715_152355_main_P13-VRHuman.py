# User P13-VRHuman main code file  
n= 30
f =[0,1]
kq =0
for i in range(2,n+1):
    kq = f[i-1]+f[i-2]
    f.append(kq)
print(f[7])