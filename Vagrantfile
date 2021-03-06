# -*- mode: ruby -*-
# vi: set ft=ruby :

unless Vagrant.has_plugin?("vagrant-vbguest")
  raise 'vagrant-vbguest is not installed!'
end

# All Vagrant configuration is done below. The "2" in Vagrant.configure
# configures the configuration version (we support older styles for
# backwards compatibility). Please don't change it unless you know what
# you're doing.
Vagrant.configure(2) do |config|

  config.vm.define "debian" do |debian|
    # Every Vagrant development environment requires a box. You can search for
    # boxes at https://atlas.hashicorp.com/search.
    debian.vm.box = "generic/debian9"
    debian.vm.hostname = 'audio-analysis-debian'

    # Create a forwarded port mapping which allows access to a specific port
    # within the machine from a port on the host machine. In the example below,
    # accessing "localhost:8080" will access port 80 on the guest machine.
    # config.vm.network "forwarded_port", guest: 80, host: 8080

    # Create a private network, which allows host-only access to the machine
    # using a specific IP.
    # config.vm.network "private_network", ip: "192.168.33.10"

    # Create a public network, which generally matched to bridged network.
    # Bridged networks make the machine appear as another physical device on
    # your network.
    # config.vm.network "public_network"

    # Share an additional folder to the guest VM. The first argument is
    # the path on the host to the actual folder. The second argument is
    # the path on the guest to mount the folder. And the optional third
    # argument is a set of non-required options.
    debian.vm.synced_folder ".", "/vagrant", type: "virtualbox"
    debian.vm.synced_folder ".", "/home/vagrant/audio-analysis", type: "virtualbox"
    
    debian.vm.provider "hyperv" do |hyperv, override|
      hyperv.vmname = 'audio-analysis-debian'
      hyperv.memory = "8096"
      hyperv.cpus = 4


      override.vm.synced_folder "./", "/vagrant", type: "smb", mount_options: ["vers=3.0"]
      override.vm.synced_folder "./", "/home/vagrant/audio-analysis", type: "smb", mount_options: ["vers=3.0"]
    end    
    
    debian.vm.provider "virtualbox" do |vb|
      vb.gui = false
      vb.name = 'audio-analysis-debian'
      #vb.memory = "8096"
      #vb.cpus = 4
    end


    # Enable provisioning with a shell script. Additional provisioners such as
    # Puppet, Chef, Ansible, Salt, and Docker are also available. Please see the
    # documentation for more information about their specific syntax and use.
    config.vm.provision "shell", path: "build/mono_install.sh"
    
    # setup audio tools
    config.vm.provision "ansible_local" do |ansible|
      ansible.playbook = "build/audio_tools.yml"
      ansible.install_mode = :pip
    end
  end
  
 
end
