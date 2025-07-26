# User P18-VC main code file  
n = 8
arr= [0,1]
for i in range (n-1):
    val = arr[i] + arr[i+1]
    arr.append(val)

print(arr[n])