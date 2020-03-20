function Format-Maa-Policy {
    Param ($Policy)

    $bodytext = '{"AttestationPolicy": "' + $Policy + '"}'

    $header = Encode-Base64Url '{"alg":"none"}'
    $body =   Encode-Base64Url $bodytext

    return "$header.$body."
}

function Encode-Base64Url {
    Param ($unencoded)

    $base64encoded = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($unencoded))
    return $base64encoded.Split('=')[0].Replace('+','-').Replace('/','_')
}

