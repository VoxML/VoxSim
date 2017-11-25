import socket
import struct
import sys
import time
import random
import re
from datetime import datetime

def generate_line():
   global f 
   global index_time

   content = ''
   wait_time = 0
   if f is not None:
       line = f.readline()
       if re.match(r"^\d+.*\t",line):
           if index_time == 0:
               index_time = float(re.match(r"^\d+.*\t",line).group(0).rstrip())-2
	   wait_time = float(re.match(r"^\d+.*\t",line).group(0).rstrip())-index_time
	   index_time += wait_time
           #print (index_time,wait_time)
           content = re.sub(r"^\d+.*\t","",line)
       elif line == '':
           f.close()
           print "Script complete.  Shutting down server."
           exit(0)
       else:
           content = line
   else:
       content = raw_input('Command: ')

   new_state = content.rstrip()
   #ts = datetime.fromtimestamp(time.time()).strftime("%M:%S:%f")[:-3]
   ts = "{0:.3f}".format(time.time())
   data_to_send = new_state
   if not re.search(r";\d+.\d{3}$",data_to_send):
      data_to_send += ";" + ts  #attaching timestamp to the data before sending
   #print data_to_send
   return (data_to_send,wait_time)


if __name__=="__main__":
	global f
        global index_time

	host = 'localhost'
	port = 8220
	address = (host, port) #Initializing the port and the host for the connection

	server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
	server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
	server_socket.bind(address)
	server_socket.listen(5) #Setting up connection with the client and listening

	print "Listening for client . . ."
	conn, address = server_socket.accept()
	print "Connected to client at ", address
	#pick a large output buffer size because i dont necessarily know how big the incoming packet is   

	f = None
        index_time = 0
	if len(sys.argv) > 1:
	    file_name = sys.argv[1]
	    if file_name is not None:
	        f = open(file_name,'r')
		                                         
	while True:  #continuously generate line from the file and send to the client
	    try: 
               msg_to_send = generate_line()  
               if msg_to_send is not ('',0):
                   time.sleep(msg_to_send[1])
                   conn.send(struct.pack("<i" + str(len(msg_to_send[0])) + "s", len(msg_to_send[0]), msg_to_send[0]))
	           print msg_to_send[0]
               else:
                   break
	       #time.sleep(random.randint(3,3))
	    except (KeyboardInterrupt, SystemExit):
	       msg_to_send = "shutting down server"
               conn.send(msg_to_send)
	       break
	    
        conn.close()
	server_socket.close()
	sys.exit("Shutting down.")
