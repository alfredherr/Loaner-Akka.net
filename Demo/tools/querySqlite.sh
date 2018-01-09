#!/bin/bash

function ProgressBar {
let _progress=(${1}*100/${2}*100)/100
let _done=(${_progress}*4)/10
let _left=40-$_done
_fill=$(printf "%${_done}s")
_empty=$(printf "%${_left}s")

printf "\r\t\t\t[${_fill// /#}${_empty// /-}] ${_progress}%%"

}
_start=1
_end=100

r=$(sqlite3 ../akka_demo.db "select count(distinct persistence_id) from event_journal;" )
echo "$r records processed. calculating elapsed time and approximate rate..."
STARTTIME=$(date +%s);
START=$(date +%s);
LASTRECORDCOUNT=$r
sleep 5;
while :
do
	r=$(sqlite3 ../akka_demo.db "select count(distinct persistence_id) from event_journal;" )
	if [ -n $r -a $r -ge 1 ];
	then

		END=$(date +%s);
		recdifference=$(($r-$LASTRECORDCOUNT))
		timedifference=$(($(date +'%s') - $STARTTIME))
		absolutetimedifference=$(($(date +'%s') - $START))
		rate=$(echo $(($recdifference/$timedifference)) | bc -l)
		echo "$r records processed." 
		echo "elapsed time: $absolutetimedifference seconds".
		echo -ne "~rate: $rate per second.  "
		LASTRECORDCOUNT=$r
		STARTTIME=$END
		for number in $(seq ${_start} ${_end})
		do
			sleep 0.1
			ProgressBar ${number} ${_end}
		done
	else 
		for number in $(seq ${_start} ${_end})
		do
			sleep 0.1
			ProgressBar ${number} ${_end}
		done
	fi
	echo ""
done
