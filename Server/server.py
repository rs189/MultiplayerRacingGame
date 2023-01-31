import threading
import asyncio
import random
import sqlite3
import os
import time

class CustomMultiplayer:
    def __init__(self):
        self.uids = []

        self.spawn_points = [
            (6.204,9.58199978,231.190002),
            (12.0100002,9.58199978,231.190002),
            (12.0100002,9.58199978,226.190002),
            (6.204,9.58199978,226.190002),
        ]
        self.spawn_index = 0

        self.positions = {}
        self.safe_positions = {}

custom_multiplayer = CustomMultiplayer()

class EchoServerProtocol:
    def connection_made(self, transport):
        self.transport = transport

        self.addresses = []
        self.safe_addresses = []

        db_path = 'vehicles'
        db_path = os.path.join(os.path.dirname(__file__), db_path)
        self.train_database = sqlite3.connect(db_path, check_same_thread=False)
        
        #a = threading.Thread(target=self.update_thread)
        #a.start()

    def save_record(self, name, timer):
        timestamp = int(time.time())
        cursor = self.train_database.cursor()
        cursor.execute("INSERT INTO leaderboard (player_name, player_time, timestamp) VALUES (?, ?, ?)", (name, timer, timestamp))
        self.train_database.commit()

    def get_records(self):
        cursor = self.train_database.cursor()
        cursor.execute("SELECT player_name, player_time FROM leaderboard ORDER BY player_time ASC LIMIT 10")
        return cursor.fetchall()

    def update_thread(self):
        #while True:
        for position in custom_multiplayer.safe_positions.values():
            uid = position['uid']
            x = position['x']
            y = position['y']
            z = position['z']
            rx = position['rx']
            ry = position['ry']
            rz = position['rz']
            order = position['order']
            response = f'PhysicsUpdate:{uid}:{x}:{y}:{z}:{rx}:{ry}:{rz}:{order}'
            #print(f"Sent {response} to {address}")
            #print(response)
            for address in self.safe_addresses:
                self.transport.sendto(response.encode(), address)
        #time.sleep(0.1)
        custom_multiplayer.safe_positions = custom_multiplayer.positions.copy()
        self.safe_addresses = self.addresses.copy()

    def datagram_received(self, data, addr):
        message = data.decode()
        #print('Received %r from %s' % (message, addr))

        if addr not in self.addresses:
            self.addresses.append(addr)

        # GetRecords
        if message.startswith('GetRecords'):
            records = self.get_records()
            records = ';'.join([f'{name}:{timer}' for name, timer in records])
            response = f'Records:{str(records)};'        
            self.transport.sendto(response.encode(), addr)
            print(f"Sent records to {addr}")

        # SaveRecord:{name}:{timer}
        if message.startswith('SaveRecord:'):
            name, timer = message.split(':')[1:]
            self.save_record(name, timer)
            print(f"Saved record {name} {timer}")

            records = self.get_records()
            records = ';'.join([f'{name}:{timer}' for name, timer in records])
            response = f'Records:{str(records)};' 
            for address in self.addresses:       
                self.transport.sendto(response.encode(), address)
                print(f"Sent records to {address}")

        # Spawn:{uid}
        if message.startswith('Spawn:'):
            uid = message.split(':')[1]
            custom_multiplayer.uids.append(uid)
            if custom_multiplayer.spawn_index >= len(custom_multiplayer.spawn_points):
                custom_multiplayer.spawn_index = 0
            position = custom_multiplayer.spawn_points[custom_multiplayer.spawn_index] # TODO: Instead of index use player uid
            custom_multiplayer.spawn_index += 1
            response = f'Spawned:{uid}:{position}'
            self.transport.sendto(response.encode(), addr)
            print(f"Spawned {uid} at {position}")
            if uid not in custom_multiplayer.positions:
                custom_multiplayer.positions[uid] = {
                    'uid': uid,
                    'x': position[0],
                    'y': position[1],
                    'z': position[2],
                    'rx': 0,
                    'ry': 0,
                    'rz': 0,
                    'order': 0,
                }

        # PhysicsUpdate:{uid}:{x}:{y}:{z}:{rx}:{ry}:{rz}
        elif message.startswith('PhysicsUpdate:'):
            uid = message.split(':')[1]
            x = message.split(':')[2]
            y = message.split(':')[3]
            z = message.split(':')[4]
            rx = message.split(':')[5]
            ry = message.split(':')[6]
            rz = message.split(':')[7]
            if uid not in custom_multiplayer.positions:
                custom_multiplayer.positions[uid] = {
                    'uid': uid,
                    'x': x,
                    'y': y,
                    'z': z,
                    'rx': rx,
                    'ry': ry,
                    'rz': rz,
                    'order': 0,
                }
            if uid in custom_multiplayer.positions:
                custom_multiplayer.positions[uid].update({
                    'x': x,
                    'y': y,
                    'z': z,
                    'rx': rx,
                    'ry': ry,
                    'rz': rz,
                    'order': custom_multiplayer.positions[uid]['order'] + 1,
                })
                
            self.update_thread()
            #for uid, position in custom_multiplayer.positions.items():
            #    x, y, z, rx, ry, rz = position
            #    response = f'PhysicsUpdate:{uid}:{x}:{y}:{z}:{rx}:{ry}:{rz}:{position["order"]}'
            #    self.transport.sendto(response.encode(), address)
    
    def error_received(self, exc):
        if isinstance(exc, ConnectionResetError):
            address = self.transport.get_extra_info("peername")
            self.addresses.remove(address)
            print(f"Removed disconnected client at {address}")
            
    def connection_lost(self, exc):
        if isinstance(exc, ConnectionResetError):
            address = self.transport.get_extra_info("peername")
            self.addresses.remove(address)
            print(f"Removed disconnected client at {address}")

if __name__ == '__main__':
    loop = asyncio.get_event_loop()
    print("Starting UDP server")

    listen = loop.create_datagram_endpoint(EchoServerProtocol, local_addr=('127.0.0.1', 12000))
    transport, protocol = loop.run_until_complete(listen)

    try:
        loop.run_forever()
    except KeyboardInterrupt:
        pass

    transport.close()
    loop.close()