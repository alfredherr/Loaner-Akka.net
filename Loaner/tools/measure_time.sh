#!/bin/bash

FILESIZE=$1
CURRENTSIZE="$(wc -c db/*|grep -i total|awk '{print $1}')"
START=$(date +%s)

while [ $CURRENTSIZE -lt $FILESIZE ]
do
	END=$(date +%s)
	DIFF=$(( $END - $START ))
	MINUTES=$(($DIFF / "60"))
	echo "$CURRENTSIZE out of $FILESIZE .... elapsed time $MINUTES minutes ($DIFF seconds)"
	sleep 3
	CURRENTSIZE="$(wc -c db/*|grep -i total|awk '{print $1}')"
done	
END=$(date +%s)
DIFF=$(( $END - $START ))
MINUTES=$(($DIFF / "60"))
echo "Done! It took $MINUTES minutes ($DIFF seconds)"
