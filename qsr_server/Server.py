from __future__ import print_function, division
from strands_qsr_lib.qsr_lib.src.qsrlib.qsrlib import QSRlib, QSRlib_Request_Message
from qsrlib_io.world_trace import World_Trace
import argparse
import socket
import struct
import time
import re
from collections import defaultdict
import sys


# activate the virturalenv: source venv/bin/activate
# run python Server.py, the following should appear
# ('localhost', 8220)
# Listening for client . . .
# attach the client.cs file to a unity GameObject and click play


def pretty_print_world_qsr_trace(which_qsr, qsrlib_response_message):
    print(which_qsr, "request was made at ", str(qsrlib_response_message.req_made_at)
          + " and received at " + str(qsrlib_response_message.req_received_at)
          + " and finished at " + str(qsrlib_response_message.req_finished_at)
          + "and the qstag is " + str(qsrlib_response_message.qstag))
    print("---")
    print("Response is:")
    for t in qsrlib_response_message.qsrs.get_sorted_timestamps():
        foo = str(t) + ": "
        for k, v in zip(qsrlib_response_message.qsrs.trace[t].qsrs.keys(),
                        qsrlib_response_message.qsrs.trace[t].qsrs.values()):
            foo += str(k) + ":" + str(v.qsr) + "; "
        print(foo)


def qsr_wrapper(str, which_qsr="rcc8"):
    # create a QSRlib object
    qsrlib = QSRlib()
    # convert your data in to QSRlib standard input format World_Trace object
    world = World_Trace()

    # convert from a string looks like this:
    # knife 0.8001107 -0.0009521564 0.0002398697 1 1 1	cup 0 0 0 1 1 1
    # knife 0.9612383 0.002230517 0.005103105 1 1 1	cup 0 0 0 1 1 1
    obj_dict = defaultdict(list)
    lines = str.split('\n')
    for line in lines:
        if not line.strip():
            continue
        obj0_str, obj1_str = line.strip().split(',')
        obj0_trace = obj0_str.split()
        obj1_trace = obj1_str.split()
        obj_dict[obj0_trace[0]].append(tuple(float(n) for n in obj0_trace[1:]))
        obj_dict[obj1_trace[0]].append(tuple(float(n) for n in obj1_trace[1:]))

    for obj, points in obj_dict.items():
        world.add_object_track_from_list(points, obj)


    # make a QSRlib request message
    # dynammic_args = {'argd': {"qsr_relations_and_values" : {"Touch": 0.5, "Near": 6, "Far": 10}}}
    # dynammic_args = {"qtcbs": {"no_collapse": True, "quantisation_factor":0.01, "validate":False, "qsrs_for":[("o1","o2")] }}
    dynammic_args={"tpcc":{"qsrs_for":[("o1","o2","o3")] }}

    qsrlib_request_message = QSRlib_Request_Message(which_qsr, world, dynammic_args)
    # request your QSRs
    qsrlib_response_message = qsrlib.request_qsrs(req_msg=qsrlib_request_message)
    # print out your QSRs
    pretty_print_world_qsr_trace(which_qsr, qsrlib_response_message)


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


def server_setup():
    parser = argparse.ArgumentParser(
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
        description=__doc__
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
    # pick a large output buffer size because i dont necessarily know how big the incoming packet is

    f = None
    index_time = 0

    file_name = args.file
    if file_name is not '':
        f = open(file_name, 'r')
    while True:  # continuously generate line from the file and send to the client
        try:
            msg_to_send = generate_line()  # ('G;attentive start;1566398170.253', 0)
            if msg_to_send is not ('', 0):
                time.sleep(msg_to_send[1])
                print("msg_to_send:" + msg_to_send[0] + " wait:" + str(msg_to_send[1]) + " sec")
                # msg_to_send:G;attentive start;1566398170.253 wait:0 sec

                if msg_to_send[0] is not '':
                    conn.send(struct.pack("<i" + str(len(msg_to_send[0])) + "s", len(msg_to_send[0]),
                                          msg_to_send[0].encode('utf-8')))
                    # b'\x1d\x00\x00\x00G;engage start;1566401099.641'
                    # struct.pack Return a bytes object containing the values v1, v2, ... packed according
                    #     to the format string fmt: <iG;attentive start;1566398170.253s.
                    print(msg_to_send[0])
                    received_msg = conn.recv(1024)
                    print("*********************")
                    print(received_msg)
                    qsr_wrapper(received_msg)
                # else:
                #    print("breaking")
                #    break
                # time.sleep(random.randint(3,3))
        except (KeyboardInterrupt, SystemExit):
            msg_to_send = "shutting down server"
            conn.send(msg_to_send.encode('utf-8'))
            break

    conn.close()
    server_socket.close()
    sys.exit("Shutting down.")


if __name__ == "__main__":
    server_setup()