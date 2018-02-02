#!/bin/bash

x=5

while [ $x -gt 0 ]

do

    sleep 1s

    #clear

    echo "$x seconds until blast off"

    x=$(( $x - 1 ))

done
