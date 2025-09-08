<img src="https://docs.google.com/drawings/d/e/2PACX-1vQIu2YzN5UrNI_6hsp3XvNlP9ZkdJp61I4dQnXhyX_ZSa0NT-Rn2Gw3J3a8cekAk29r1EayUkVlps8u/pub?w=960&amp;h=720">

## 📝 Table of Contents

- [Setup Keys](#setup_keys)
- [Rotate Signing Keys](#rotate_keys)
- [Token Validation](#token_validation)

## Setup Keys <a name = "setup_keys"></a>
Generate your RSA key with 2048 bit minimum length, store it in Database "member", collection "signing_credential_keys", mark it as a main key, **dont forget to generate a key id for the key**, example:

```json
{
    "privateKey":"your RSA private key",
    "isMain":true,
    "keyId":"your Key Id"
}
```

**How does it work?**

When startup, the identity server will fetch the private keys from database, the key with isMain = true will be signing key, it means that all tokens will be generated from this key. Others key with isMain=false will be validation keys.

## Rotate Signing Keys <a name = "rotate_keys"></a>
Its recommened that after a period of time, we should rotate the signing key. This is the process of retiring one key and onboarding another.
Lets say we are using key1 as signing key, now we want to change the signing key to key2, but we can't do that immediately. The reason is that normally the consumers (aka the Api Resource in the diagram) will cache your identity server's validation keys (RSA public key in json web key format) from discovery document. If you change immediately, the new tokens being signed with key2 will be sent to consumers that have only key1 in their cache, result with 401 Unauthorized. What we need to do is adding the key2 as validation key, we will leave it running after some amount of time to allow consumers to update their validation keys in their cache. Then you can switch over and promote key2 to be your signing key, the key 1 will be validation key and only used for token validation. Then after some more amount of time (longer than the expiration of any token being signed with key1), you can finally remove key1.

Take a look at SigningCredentialStore.cs for more details about how to fetch signing keys and validation keys from database.

## Token Validation <a name = "token_validation"></a>

As you know, when you send the discovery request to identity server, the response looks like this:

```json
{
    "issuer":"http://auth.lumigame.com",
    "jwks_uri":"http://auth.lumigame.com/.well-known/openid-configuration/jwks",
    "authorization_endpoint":"http://auth.lumigame.com/connect/authorize",
    "token_endpoint":"http://auth.lumigame.com/connect/token",
    "userinfo_endpoint":"http://auth.lumigame.com/connect/userinfo",
    "end_session_endpoint":"http://auth.lumigame.com/connect/endsession",
    "check_session_iframe":"http://auth.lumigame.com/connect/checksession",
    "revocation_endpoint":"http://auth.lumigame.com/connect/revocation",
    "introspection_endpoint":"http://auth.lumigame.com/connect/introspect",
    "device_authorization_endpoint":"http://auth.lumigame.com/connect/deviceauthorization",
....
}
```
Lets say you have 1 signing key and 1 validation key, in order to validate the token, the consumer will use the jwks_uri value, send the request to get all validation keys, the response looks like this

```json
{
    "keys":[
        {
            "kty":"RSA",
            "use":"sig",
            "kid":"key1's KeyId",
            "e":"...",
            "n":"",
            "alg":"RS256"
        },
                {
            "kty":"RSA",
            "use":"sig",
            "kid":"key2's KeyId",
            "e":"...",
            "n":"...",
            "alg":"RS256"
        }
    ]
}
```

The consumer will use these keys to validation tokens being signed by either key1 or key2

