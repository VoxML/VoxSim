from __future__ import print_function, division
import sys,os
#sys.path.insert(0,'/Users/nikhil/Documents/Work/Brandeis/Dissertation/trunk/VoxSim/Assets/Externals/python/qsr_example/qsr_mwe/strands_qsr_lib/qsr_lib/src')
sys.path.append(os.path.join(sys.path[0],'strands_qsr_lib','qsr_lib','src'))
print(sys.path)

from strands_qsr_lib.qsr_lib.src.qsrlib.qsrlib import QSRlib, QSRlib_Request_Message
from qsrlib_io.world_trace import World_Trace
import argparse
import socket
import struct
import time
import re
from collections import defaultdict
import random


def pretty_print_world_qsr_trace(which_qsr, qsrlib_response_message):
    print(which_qsr + " request was made at " + str(qsrlib_response_message.req_made_at) \
                 + " and received at " + str(qsrlib_response_message.req_received_at)\
                 + " and finished at " + str(qsrlib_response_message.req_finished_at)\
                 + "and the qstag is " + str(qsrlib_response_message.qstag) \
                 + "\n" + "---" + "\n" + "Response is :")
    # print(which_qsr, "request was made at ", str(qsrlib_response_message.req_made_at)
    #       + " and received at " + str(qsrlib_response_message.req_received_at)
    #       + " and finished at " + str(qsrlib_response_message.req_finished_at)
    #       + "and the qstag is " + str(qsrlib_response_message.qstag))
    # print("---")
    # print("Response is:")

    qsr_result = ""
    for t in qsrlib_response_message.qsrs.get_sorted_timestamps():
        foo = str(t) + ": "
        for k, v in zip(qsrlib_response_message.qsrs.trace[t].qsrs.keys(),
                        qsrlib_response_message.qsrs.trace[t].qsrs.values()):
            foo += str(k) + ":" + str(v.qsr) + "; "
        qsr_result += foo
    print(qsr_result)
    return qsr_result

def qsr_wrapper(str):
    qsrlib = QSRlib()

    print(str)
    # convert your data in to QSRlib standard input format World_Trace object
    world = World_Trace()
    # convert from a string looks like this:
    # knife 0.8001107 -0.0009521564 0.0002398697 1 1 1	cup 0 0 0 1 1 1
    # knife 0.9612383 0.002230517 0.005103105 1 1 1	cup 0 0 0 1 1 1
    obj_dict = defaultdict(list)
    
    which_qsr = str.split(':')[0]
    lines = str.split(':')[1].split('\n')
    for line in lines:
        if not line.strip():
            continue

        # obj0_str, obj1_str, obj2_str = line.strip().split(',')

        for obj_str in line.strip().split(','):
            obj_trace = obj_str.split()
            obj_dict[obj_trace[0]].append(tuple(float(n) for n in obj_trace[1:]))

        # obj0_trace = obj0_str.split()
        # obj1_trace = obj1_str.split()
        # obj2_trace = obj2_str.split()
        # obj_dict[obj0_trace[0]].append(tuple(float(n) for n in obj0_trace[1:]))
        # obj_dict[obj1_trace[0]].append(tuple(float(n) for n in obj1_trace[1:]))
        # obj_dict[obj2_trace[0]].append(tuple(float(n) for n in obj2_trace[1:]))
    for obj, points in obj_dict.items():
        world.add_object_track_from_list(points, obj)


    qsr_result = ""

    # for which_qsr in ['rcc8', 'tpcc']:
    #for which_qsr in ['mos', 'cardir', 'rcc2', 'rcc3', 'rcc4', 'rcc5', 'rcc8', 'ra', 'mwe', 'tpcc', 'argd', 'argprobd', 'qtccs', 'qtcbcs',  'qtcbs' ]:
    # for which_qsr in ['mos', 'cardir', 'rcc2', 'rcc3', 'rcc4', 'rcc5', 'rcc8', 'ra', 'mwe', 'tpcc', 'argd', 'argprobd' ]:
    if which_qsr in ['mos', 'cardir','rcc2', 'rcc3', 'rcc4', 'rcc5', 'rcc8', 'ra',  'mwe', 'qtccs',  'qtcbcs']:
        dynammic_args = dict()
    elif which_qsr in ['argd']:
        dynammic_args = {'argd': {"qsr_relations_and_values" : {"Touch": 0.5, "Near": 6, "Far": 10}}}
    elif which_qsr in ['argprobd']:
        dynammic_args = {'argprobd': {"qsr_relations_and_values": {"Touch": (0.5, 0.5), "Near": (6, 6), "Far": (10, 10)}}}
    elif which_qsr in ['qtcbs']:
        # dynammic_args = {"qtcbs": {"no_collapse": True, "quantisation_factor":0.01, "validate": False, "qsrs_for":[(obj0_trace[0],obj1_trace[0])] }}
        dynammic_args = {"qtcbs": {"no_collapse": True, "quantisation_factor":0.01, "validate": False, "qsrs_for":[tuple(obj_dict.keys())] }}
    elif which_qsr in ['qtccs']:
        dynammic_args = {"qtccs": {"no_collapse": True, "quantisation_factor":0.01, "validate": False, "qsrs_for":[tuple(obj_dict.keys())] }}
    elif which_qsr in ['qtcbcs']:
        dynammic_args = {"qtcbcs": {"no_collapse": True, "quantisation_factor":0.01, "validate": False, "qsrs_for":[tuple(obj_dict.keys())] }}
    elif which_qsr in ['tpcc']:
        # dynammic_args={"tpcc":{"qsrs_for":[(obj0_trace[0], obj1_trace[0], obj2_trace[0])]}}
        dynammic_args = {"tpcc": {"qsrs_for": [tuple(obj_dict.keys())]}}


    # make a QSRlib request message
    qsrlib_request_message = QSRlib_Request_Message(which_qsr, world, dynammic_args)
    # request your QSRs
    qsrlib_response_message = qsrlib.request_qsrs(req_msg=qsrlib_request_message)
    # print out your QSRs
    qsr_result += pretty_print_world_qsr_trace(which_qsr, qsrlib_response_message)
    qsr_result += "\n**************************************\n"
    return qsr_result

def generate_line():
    global f
    global index_time
    global timestamps
    content = ''
    wait_time = 0
    if f is not None:
        line = f.readline()
        if re.match(r"^\d+\t", line):
            print(line)
            if index_time == 0:
                print(re.search(r"\t\d+.\d+", line))
                index_time = float(re.search(r"\t\d+.\d+", line).group(0).rstrip()) - 2
            wait_time = float(re.search(r"\t\d+.\d+", line).group(0).rstrip()) - index_time
            index_time += wait_time
            print((index_time, wait_time))
            if re.split(r'\t', line)[1].startswith('H'):
                content = re.split(r'\t', line)[1].replace('H', '') + ';' + re.split(r'\t', line)[2]
        elif line == '':
            f.close()
            print("Script complete.  Shutting down server.")
            exit(0)
        else:
            content = line
    else:
        print('Command: ', end='')
        content = sys.stdin.readline() # G;attentive start

    new_state = content.rstrip()  # G;attentive start
    # ts = datetime.fromtimestamp(time.time()).strftime("%M:%S:%f")[:-3]
    ts = "{0:.3f}".format(time.time()) # 1566403989.607
    data_to_send = new_state
    if not re.search(r";\d+.\d{3}$", data_to_send) and data_to_send is not '' and timestamps:
        data_to_send += ";" + ts  # attaching timestamp to the data before sending
    print((data_to_send, wait_time))  # ('G;attentive start;1566398170.253', 0)
    return (data_to_send, wait_time)

def send_msg(conn, msg):
    msg = struct.pack("<i" + str(len(msg)) + "s", len(msg), msg.encode('utf-8'))
    # b'\x1d\x00\x00\x00G;engage start;1566401099.641'
    # struct.pack Return a bytes object containing the values v1, v2, ... packed according
    #     to the format string fmt: <iG;attentive start;1566398170.253s.
    conn.send(msg)

def recv_msg(conn):
    # Read message length and unpack it into an integer
    raw_msglen = conn.recv(4)
    if not raw_msglen:
        return None
    msglen = struct.unpack('<i', raw_msglen)[0]
    # Read the message data
    return conn.recv(msglen)

def server_setup():
    parser = argparse.ArgumentParser(
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
        description=__doc__
    )
    parser.add_argument(
        '-q', '--qsr',
        default="rcc8",
        type=str,
        action='store',
        nargs='?',
        help='Specify qsr id to run the app.'
    )
    parser.add_argument(
        '-p', '--port',
        default=8220,  # 8220 is the port for TCP, this is the listening port of server
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
    whichqsr = args.qsr
    timestamps = args.timestamps
    # address = (host, port) #Initializing the port and the host for the connection
    print((host, port))

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind((host, port))
    # the parameter 1 in listen(1) is backlog, backlog is how many pending connections can exist
    server_socket.listen(1)  # Setting up connection with the client and listening

    print("Listening for client . . .")
    conn, address = server_socket.accept()
    print("Connected to client at ", address)  # ('127.0.0.1', 59695)

    f = None
    index_time = 0

    file_name = args.file
    if file_name is not '':
        f = open(file_name, 'r')

    i = 0
    while True:  # continuously generate line from the file and send to the client
        #msg_to_send = generate_line()  # ('G;attentive start;1566398170.253', 0)
        try:
            #if msg_to_send[0] != '':
            #    time.sleep(msg_to_send[1])
            #    print("msg_to_send:" + msg_to_send[0] + " wait:" + str(msg_to_send[1]) + " sec")
                # msg_to_send:G;attentive start;1566398170.253 wait:0 sec

                # if msg_to_send[0] is not '':
                #    print("the message sent is: " + msg_to_send[0].split(";")[0].encode('utf-8'))
                #send_msg(conn, (msg_to_send[0].split(";")[0].encode('utf-8')))
            received_msg = recv_msg(conn)
            print("received_msg is " + received_msg)
            qsr_result = qsr_wrapper(received_msg)
            send_msg(conn, qsr_result.encode('utf-8'))
                    #else:
                    #print("breaking")
                    #break
            time.sleep(random.randint(3,3))
            i += 1
            print("Above is loop number " + str(i))
            print("***********************************************")

        except (KeyboardInterrupt, SystemExit):
            msg_to_send = "shutting down server"
            conn.send(msg_to_send.encode('utf-8'))
            break

    conn.close()
    server_socket.close()
    sys.exit("Shutting down.")


# def recv_msg(conn):
#     # Read message length and unpack it into an integer
#     raw_msglen = recvall(conn, 4)
#     if not raw_msglen:
#         return None
#     msglen = struct.unpack('<i', raw_msglen)[0]
#     # Read the message data
#     return recvall(conn, msglen)
#
# def recvall(conn, n):
#     # Helper function to recv n bytes or return None if EOF is hit
#     data = b''
#     while len(data) < n:
#         packet = conn.recv(n - len(data))
#         if not packet:
#             return None
#         data += packet
#     return data

if __name__ == "__main__":
    server_setup()
