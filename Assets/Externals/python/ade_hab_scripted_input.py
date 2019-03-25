import argparse
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
    global timestamps
    content = ''
    wait_time = 0
    if f is not None:
        line = f.readline()
        if re.match(r"^\d+\t",line):
            print(line)
            if index_time == 0:
                print(re.search(r"\t\d+.\d+",line)) 
                index_time = float(re.search(r"\t\d+.\d+",line).group(0).rstrip()) - 2
            wait_time = float(re.search(r"\t\d+.\d+",line).group(0).rstrip()) - index_time
            index_time += wait_time
            print((index_time,wait_time))
            if re.split(r'\t',line)[1].startswith('H'):
                content = re.split(r'\t',line)[1].replace('H','') + ';' + re.split(r'\t',line)[2]
        elif line == '':
            f.close()
            print("Script complete.  Shutting down server.")
            exit(0)
        else:
            content = line
    else:
        print('Command: ', end='')
        content = sys.stdin.readline()

    new_state = content.rstrip()
    #ts = datetime.fromtimestamp(time.time()).strftime("%M:%S:%f")[:-3]
    ts = "{0:.3f}".format(time.time())
    data_to_send = new_state
    if not re.search(r";\d+.\d{3}$",data_to_send) and data_to_send is not '' and timestamps:
        data_to_send += ";" + ts  #attaching timestamp to the data before sending
    print((data_to_send,wait_time))
    return (data_to_send,wait_time)


if __name__=="__main__":
    parser = argparse.ArgumentParser(
                                     formatter_class=argparse.ArgumentDefaultsHelpFormatter,
                                     description=__doc__
                                     )
    parser.add_argument(
                     '-p', '--port',
                     default=4444,
                     type=int,
                     action='store',
                     nargs='?',
                     help='Specify port number to run the app.'
                     )
    parser.add_argument(
                     '-s', '--host',
                     default='localhost',
                     action='store',
                     nargs='?',
                     help='Specify host name for app to run on.'
                     )
    parser.add_argument(
                     '-f', '--file',
                     default='',
                     action='store',
                     nargs='?',
                     help='Specify input log file.'
                     )
    parser.add_argument(
                     '-t', '--timestamps',
                     default=True,
                     action='store_false',
                     help='Silence timestamps'
                     )
    args = parser.parse_args()

    global f
    global index_time
    global timestamps

    host = args.host
    port = args.port
    timestamps = args.timestamps
    #address = (host, port) #Initializing the port and the host for the connection
    print((host, port))

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind((host, port))
    server_socket.listen(1) #Setting up connection with the client and listening

    print("Listening for client . . .")
    conn, address = server_socket.accept()
    print("Connected to client at ", address)
    #pick a large output buffer size because i dont necessarily know how big the incoming packet is   

    f = None
    index_time = 0
    
    file_name = args.file
    if file_name is not '':
        f = open(file_name,'r')
    while True:  #continuously generate line from the file and send to the client
        try: 
            msg_to_send = generate_line()
            if msg_to_send is not ('',0):
                time.sleep(msg_to_send[1])
                print("msg_to_send:" + msg_to_send[0] + " wait:" + str(msg_to_send[1]) + " sec")
                if msg_to_send[0] is not '':
                    message_to_send = msg_to_send[0].encode("UTF-8")
                    conn.send(len(message_to_send).to_bytes(2, byteorder='big'))
                    conn.send(message_to_send)
                #else:
                #    print("breaking")
                #    break
                #time.sleep(random.randint(3,3))
        except (KeyboardInterrupt, SystemExit):
            msg_to_send = "shutting down server"
            conn.send(msg_to_send.encode('utf-8'))
            break
    conn.close()
    server_socket.close()
    sys.exit("Shutting down.")
