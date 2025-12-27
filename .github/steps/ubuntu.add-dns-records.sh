#!/bin/bash
hosts="/etc/hosts"
if [ ! -f "$hosts" ]; then
  echo "$hosts not found."
  exit 1
fi

sudo sed -i '$a 127.0.0.1 threemammals.com' $hosts

echo DNS-record added to $hosts
echo ------------------------
cat $hosts
echo ------------------------

ping -c 3 threemammals.com
