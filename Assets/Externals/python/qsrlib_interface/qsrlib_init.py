# Import the os module, for the os.walk function
import os
 
dirs = ['strands_qsr_lib','strands_qsr_lib/qsr_lib','strands_qsr_lib/qsr_lib/src']
for dir in dirs:
    if not os.path.exists(dir + '/__init__.py'):
        print("Adding __init__.py in " + dir)
        open(dir + '/__init__.py','w+')
