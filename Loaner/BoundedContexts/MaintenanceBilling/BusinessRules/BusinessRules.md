# Business Rules Processing
The ideas here are:
  - To make rules pluggable, easily configurable by end/business-users.
  - To keep business rules separate from processing, i.e. not tied to data/database.
  - To be able to introspect while processing, namely:
     - Have clear separation between the output of one rule and the input of the next.
     - Be able to examine the state of the account before and after each rule is applied
     - When validation fails, allow for upstream decision-making.
     
## Business Rules Processing
![alt text](Business%20Rules.png "Business Rules Application Process")


##  Processing Flow
![alt text](Billing%20Process.png "Business Rules Processing Flow")

## Configuration
Available business rules are listed in [CommandToBusinessRuleMap.txt](CommandToBusinessRuleMap.txt).

The file contains:
- Domain Command for which rule applies
- Name of Business Rule (Corresponds to C# Class)
- Comma delimited list of parameters (or 'NoParameters')
- Free-form test describing the rule


The binding of business rules to an account (or all accounts, '*') or porfolio(s) is in [BusinessRulesMap.txt](BusinessRulesMap.txt).

The file contains:

- Client-Portfolio-Account
- Command
- Rule to map
- Parameters(comma separated key value pairs)

Order matters.

Example 
- For all Greentree accounts under portfolio 'VILLADELMAR'
  1. when executing an BillingAssessment command, 
  1. verify that the account balance is not negative (AccountBalanceMustNotBeNegative), then
  1. verify that the account has at least one active obligaction on to which do the billing

```text
Greentree-VILLADELMAR-*|BillingAssessment|AccountBalanceMustNotBeNegative|NoParameters
Greentree-VILLADELMAR-*|BillingAssessment|AnObligationMustBeActiveForBilling|NoParameters
```
