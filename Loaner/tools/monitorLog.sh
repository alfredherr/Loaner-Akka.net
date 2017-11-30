#!/bin/bash


while  true;
do
	tail nlog-all*.log
	rm nlog-all*.log
	sleep 3
done
