# Frende API example usage

This example program fetches the overview of insurances of a customer.
It shows basic authentication, how to traverse HAL, and how to deserialize union types.

## Configuration
 
This example needs to be configured with 3 parameters in Program.cs:

- _clientId_: Supplied by frende
- _certificateThumbprint_: the thumbprint of the certificate sent to frende
- _birthNumber_: A birthnumber of a test customer affiliated with your clientId. Usually supplied by frende.