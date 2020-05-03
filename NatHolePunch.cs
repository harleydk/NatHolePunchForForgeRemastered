using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Frame;
using BeardedManStudios.SimpleJSON;
using System;
using System.Collections.Generic;

namespace NatHolePunchServer
{
    class NatHolePunch
    {
        private static UDPServer server = null;
        private static Dictionary<string, List<Host>> hosts = new Dictionary<string, List<Host>>();

        static void Main(string[] args)
        {
            server = new UDPServer(2048);
            System.Console.Write("Hosting nat on: ");
            System.Console.Write(BeardedManStudios.Forge.Networking.Nat.NatHolePunch.DEFAULT_NAT_SERVER_PORT);
            System.Console.Write(System.Environment.NewLine);
            server.Connect("0.0.0.0", BeardedManStudios.Forge.Networking.Nat.NatHolePunch.DEFAULT_NAT_SERVER_PORT);

            server.textMessageReceived += TextMessageReceived;

            while (true) 
            {
                // TODO: This is not a great way to keep the app running - too CPU taxing. Consider alternatives, such as https://stackoverflow.com/questions/7402146/cpu-friendly-infinite-loop.
                string read = System.Console.ReadLine().ToLower();
            }
        }

        private static void TextMessageReceived(NetworkingPlayer player, Text frame, NetWorker sender)
        {
            try
            {
                System.Console.WriteLine($"TextMessageReceived() was called.");
                System.Console.WriteLine($"Call came from player with network-id {player.NetworkId}");
                System.Console.WriteLine($"Call data were as follows:");
                System.Console.WriteLine($"{frame.ToString()}");

                var json = JSON.Parse(frame.ToString());

                if (json["register"] != null)
                {
                    string address = player.IPEndPointHandle.Address.ToString();
                    ushort port = json["register"]["port"].AsUShort;

                    if (!hosts.ContainsKey(address))
                        hosts.Add(address, new List<Host>());

                    if (CheckAndUpdateRegisteredHost(player, address, port))
                        return;

                    RegisterNewHost(player, address, port);
                }
                else if (json["host"] != null && json["port"] != null)
                {
                    server.Disconnect(player, false);

                    string addresss = json["host"];
                    ushort port = json["port"].AsUShort;
                    ushort listeningPort = json["clientPort"].AsUShort;

                    addresss = NetWorker.ResolveHost(addresss, port).Address.ToString();

                    if (!hosts.ContainsKey(addresss))
                        return;

                    Host foundHost = new Host();
                    foreach (Host iHost in hosts[addresss])
                    {
                        if (iHost.port == port)
                        {
                            foundHost = iHost;
                            break;
                        }
                    }


                    if (string.IsNullOrEmpty(foundHost.host))
                        return;

                    JSONNode obj = JSONNode.Parse("{}");
                    obj.Add("host", new JSONData(player.IPEndPointHandle.Address.ToString().Split(':')[0]));
                    obj.Add("port", new JSONData(listeningPort));

                    JSONClass sendObj = new JSONClass();
                    sendObj.Add("nat", obj);

                    Text notifyFrame = Text.CreateFromString(server.Time.Timestep, sendObj.ToString(), false, Receivers.Target, MessageGroupIds.NAT_ROUTE_REQUEST, false);

                    server.Send(foundHost.player, notifyFrame, true);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"An error occured in call to TextMessageReceived().");
                System.Console.WriteLine($"Call came from player with network-id {player.NetworkId}.");
                System.Console.WriteLine($"Error was '{ex.Message}'.");
                System.Console.WriteLine($"Calling server.Disconnect().");

                server.Disconnect(player, true);
            }
        }

        private static void RegisterNewHost(NetworkingPlayer player, string address, ushort port)
        {
            System.Console.WriteLine($"RegisterNewHost() was called.");
            System.Console.WriteLine($"Call came from player with network-id {player.NetworkId}");
            System.Console.WriteLine($"Address and port were as follows, {address}:{port}.");

            try
            {
                System.Console.Write("Hosted Server received: ");
                System.Console.Write(address);
                System.Console.Write(":");
                System.Console.Write(port);
                System.Console.Write(" received");
                System.Console.Write(System.Environment.NewLine);

                hosts[address].Add(new Host(player, address, port));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"An error occured in call to RegisterNewHost().");
                System.Console.WriteLine($"Call came from player with network-id {player.NetworkId}");
                System.Console.WriteLine($"Error was '{ex.Message}'.");
                //throw; // refrain from throwing exception, this will halt the server.
            }
        }

        /// <summary>
        /// Check if a host has already been registered with this address and port, and update it if it has.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="address">The address of the hosting machine</param>
        /// <param name="port">The port for the hosting machine</param>
        /// <returns>True if the host was found and details updated, otherwise false</returns>
        private static bool CheckAndUpdateRegisteredHost(NetworkingPlayer player, string address, ushort port)
        {
            try
            {
                System.Console.WriteLine($"CheckAndUpdateRegisteredHost() was called.");
                System.Console.WriteLine($"Call came from player with network-id {player.NetworkId}");

                System.Console.WriteLine($"Iterating {hosts.Count} hosts.");
                for (var i = 0; i < hosts[address].Count; i++)
                {
                    var host = hosts[address][i];

                    // This host is already registered probably reconnecting so let's refresh the entry.
                    if (host.port == port)
                    {
                        System.Console.Write("Hosted Server updated: ");
                        System.Console.Write(address);
                        System.Console.Write(":");
                        System.Console.Write(port);
                        System.Console.Write(" received");
                        System.Console.Write(System.Environment.NewLine);

                        hosts[address][i] = new Host(player, address, port);

                        System.Console.WriteLine($"Host with address {address} was found, and re-instantiated.");
                        return true;
                    }
                }

                System.Console.WriteLine($"Host with address {address} was NOT found.");
                return false;

            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"An error occured in call to CheckAndUpdateRegisteredHost().");
                System.Console.WriteLine($"Call came from player with network-id {player.NetworkId}");
                System.Console.WriteLine($"Error was '{ex.Message}'.");
                //throw; // refrain from throwing exception, this will halt the server.

                return false;
            }
        }
    }
}
