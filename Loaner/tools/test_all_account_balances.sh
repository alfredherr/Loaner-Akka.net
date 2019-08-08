cat Greentree.txt|awk '{print "http://localhost/api/account/"$2"/info" }'|xargs curl -s|jq '.' |grep -i currentbalance|sort |uniq  -c
