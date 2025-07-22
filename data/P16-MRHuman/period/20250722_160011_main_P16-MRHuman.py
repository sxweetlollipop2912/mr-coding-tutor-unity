# User P16-MRHuman main code file  
x = 153
s =str(x)
n = len(s)

sum = 0
for i in s:
    sum += int(i) ** n

if (sum==x):
    print("")