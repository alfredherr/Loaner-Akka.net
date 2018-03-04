docker-compose -f docker-compose.yml down && cd Loaner/ && docker build -t alfredherr/loaner:1.16 . && cd ../ && docker-compose -f docker-compose.yml up 
