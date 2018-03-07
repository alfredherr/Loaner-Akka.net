docker-compose -f docker-compose.yml down && cd Loaner/ && docker build -t alfredherr/loaner . && cd ../ && docker-compose -f docker-compose.yml up 
