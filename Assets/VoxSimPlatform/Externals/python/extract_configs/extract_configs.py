import argparse
import os
import itertools
import sqlite3
import socket
import struct
import sys

def main():
    parser = argparse.ArgumentParser(description='Extract structural configurations')
    parser.add_argument('-s', '--structure', metavar='STRUCTURE', help='structure name')
    parser.add_argument('-d', '--database', metavar='DATABASE', help='database file')
    parser.add_argument('-x', '--exclude', metavar='EXCLUDE', nargs='+', help='object names to exclude')

    args = parser.parse_args()

    structure = args.structure
    database = args.database
    exclude_names = args.exclude
        
    connection = sqlite3.connect(database)
    
    with connection:
        cursor = connection.cursor()
            
        cursor.execute("CREATE TABLE IF NOT EXISTS Structures ( \
                       Structure TEXT, \
                       Relations TEXT)")

        host = 'localhost'
        port = 8220
        address = (host, port) #Initializing the port and the host for the connection
        
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server_socket.bind(address)
        server_socket.listen(5) #Setting up connection with the client and listening
        
        print("Listening for client . . .")
        conn, address = server_socket.accept()
        print("Connected to client at ", address)

        while True:
            try:
                data = conn.recv(2048)  #pick a large output buffer size because I don't necessarily know how big the incoming packet is
                if data:
                    config = ""
                    print("Received %s" % data)
                    for line in data.decode("utf-8").split('\n'):
                        if len(set(line.split()).intersection(set(exclude_names))) == 0:
                            config += line + "\n"
                    cursor.execute("INSERT INTO Structures VALUES (?,?)", (structure, config))
                    connection.commit()
            except (KeyboardInterrupt, SystemExit):
                msg_to_send = "shutting down server"
                conn.send(struct.pack("<i" + str(len(msg_to_send)) + "s", len(msg_to_send), msg_to_send))

        conn.close()
        server_socket.close()
        sys.exit("Shutting down.")

    connection.close()

if __name__ == "__main__":
	main()
