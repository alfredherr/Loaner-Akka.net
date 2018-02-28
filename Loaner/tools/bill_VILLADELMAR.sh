curl -X POST \
  http://localhost/api/portfolio/villadelmar/assessment \
  -H 'Cache-Control: no-cache' \
  -H 'Content-Type: application/json' \
  -H 'Postman-Token: 8ba3833b-19c1-e6cd-71e6-e2962e183bad' \
  -d '[
    {
        "item": {
            "name": "Tax",
            "amount": 10
        }
    },
    {
        "item": {
            "name": "Dues",
            "amount": 100
        }
    },
    {
        "item": {
            "name": "Reserve",
            "amount": 25
        }
    }
]'

