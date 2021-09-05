using System;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Network.Fluent.Models;

namespace AzureFluentSDK
{
    public class AzureVM
    {   
        public void CreateAzureWindowsVM()
        {
            // declare variables
            var groupName = "RG-FluentResourceGroup";
            var vmName = "VMFluent";
            var location = Region.USCentral;
            var vNetName = "VNET-Fluent";
            var vNetAddress = "172.16.0.0/16";
            var subnetName = "Subnet-Fluent";
            var subnetAddress = "172.16.0.0/24";
            var nicName = "NIC-Fluent";
            var adminUser = "azureadminuser";
            var adminPassword = "Pas$m0rd$123";
            var publicIPName = "publicIP-Fluent";
            var nsgName = "NSG-Fluent";


            var credentials = SdkContext.AzureCredentialsFactory
                    .FromFile("../../../azure-configuration.json");

            var azure = Azure.Authenticate(credentials).WithDefaultSubscription();
             
            
            Console.WriteLine($"Creating resource group {groupName} ...");
            var resourceGroup = azure.ResourceGroups.Define(groupName)
                .WithRegion(location)
                .Create();

            //Every virtual machine needs to be connected to a virtual network.
            Console.WriteLine($"Creating virtual network {vNetName} ...");
            var network = azure.Networks.Define(vNetName)
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithAddressSpace(vNetAddress)
                .WithSubnet(subnetName, subnetAddress)
                .Create();


            //You need a public IP to be able to connect to the VM from the Internet
            Console.WriteLine($"Creating public IP {publicIPName} ...");
            var publicIP = azure.PublicIPAddresses.Define(publicIPName)
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .Create();


            //You need a network security group for controlling the access to the VM
            Console.WriteLine($"Creating Network Security Group {nsgName} ...");
            var nsg = azure.NetworkSecurityGroups.Define(nsgName)
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .Create();

            //You need a security rule for allowing the
            //Internet
            Console.WriteLine($"Creating a Security Rule for allowing the remote");
            nsg.Update()
                .DefineRule("Allow-RDP")
                .AllowInbound()
                .FromAnyAddress()
                .FromAnyPort()
                .ToAnyAddress()
                .ToPort(3389)
                .WithProtocol(SecurityRuleProtocol.Tcp)
                .WithPriority(100)
                .Attach()
                .Apply();

            
            Console.WriteLine($"Creating network interface {nicName} ...");
            var nic = azure.NetworkInterfaces.Define(nicName)
                     .WithRegion(location)
                     .WithExistingResourceGroup(groupName)
                     .WithExistingPrimaryNetwork(network)
                     .WithSubnet(subnetName)
                     .WithPrimaryPrivateIPAddressDynamic()
                     .WithExistingPrimaryPublicIPAddress(publicIP)
                     .WithExistingNetworkSecurityGroup(nsg)
                     .Create();
                     

            Console.WriteLine($"Creating virtual machine {vmName} ...");
            var vm = azure.VirtualMachines.Define(vmName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetworkInterface(nic)
                    .WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer",
                        "2012-R2-Datacenter")
                    .WithAdminUsername(adminUser)
                    .WithAdminPassword(adminPassword)
                    .WithComputerName(vmName)
                    .WithSize(VirtualMachineSizeTypes.StandardDS2V2)
                    .Create();


            CheckVMStatus(azure, vm.Id);
            ShutDownVM(azure, vm.Id);

            Console.WriteLine("Successfully created a new VM: {0}!", vmName);
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }

        private void CheckVMStatus(IAzure azure, string vmID)
        {
            PowerState state = azure.VirtualMachines.GetById(vmID).PowerState;

            Console.WriteLine("Currently VM {0} is {1}", vmID, state.ToString());
        }

        private void ShutDownVM(IAzure azure, string vmID)
        {
            azure.VirtualMachines.GetById(vmID).PowerOff();

            PowerState state = azure.VirtualMachines.GetById(vmID).PowerState;

            Console.WriteLine("Currently VM {0} is {1}", vmID, state.ToString());
        }
    }
}
