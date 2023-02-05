function Format-Maa-Policy {
    Param ($Policy)

    # Create the header
    $header = Encode-Base64Url '{"alg":"none"}'

    # Create the body

    # By design, policy is double base64url encoded
    # Base64url encode #1
    $encodedpolicy =  Encode-Base64Url $Policy
    $bodytext = '{"AttestationPolicy": "' + $encodedpolicy + '"}'

    # Base64url encode #2
    $body =   Encode-Base64Url $bodytext

    # Return unsigned JWT
    return "$header.$body."
}

function Encode-Base64Url {
    Param ($unencoded)

    $base64encoded = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($unencoded))
    return $base64encoded.Split('=')[0].Replace('+','-').Replace('/','_')
}

