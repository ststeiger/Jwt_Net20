
using System.Windows.Forms;
using System.Security.Cryptography;


using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Math.EC;


// https://bitcointalk.org/index.php?topic=25141.0


// SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
// vs. 
// SHA256 sha256 = SHA256.Create();
// It has nothing to do with performance - SHA256CryptoServiceProvider uses the FIPS 140-2 validated 
// (FIPS = Federal Information Processing Standards) Crypto Service Provider (CSP) 
// while SHA256Managed does not. SHA256Managed is a pure managed implementation 
// while SHA256CryptoServiceProvider does presumably the same thing but wraps the CryptoAPI.
namespace BouncyCastleTest
{


    public partial class Form1 : Form
    {


        public Form1()
        {
            InitializeComponent();
        }

        private byte[] Base58ToByteArray(string base58)
        {

            Org.BouncyCastle.Math.BigInteger bi2 = new Org.BouncyCastle.Math.BigInteger("0");
            string b58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

            bool IgnoreChecksum = false;

            foreach (char c in base58)
            {
                if (b58.IndexOf(c) != -1)
                {
                    bi2 = bi2.Multiply(new Org.BouncyCastle.Math.BigInteger("58"));
                    bi2 = bi2.Add(new Org.BouncyCastle.Math.BigInteger(b58.IndexOf(c).ToString()));
                }
                else if (c == '?')
                {
                    IgnoreChecksum = true;
                }
                else
                {
                    return null;
                }
            }

            byte[] bb = bi2.ToByteArrayUnsigned();

            // interpret leading '1's as leading zero bytes
            foreach (char c in base58)
            {
                if (c != '1') break;
                byte[] bbb = new byte[bb.Length + 1];
                System.Array.Copy(bb, 0, bbb, 1, bb.Length);
                bb = bbb;
            }

            if (bb.Length < 4) return null;

            if (IgnoreChecksum == false)
            {


                ////////////////////////////////////SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
                SHA256 sha256 = SHA256.Create();
                // SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
                byte[] checksum = sha256.ComputeHash(bb, 0, bb.Length - 4);
                checksum = sha256.ComputeHash(checksum);
                for (int i = 0; i < 4; i++)
                {
                    if (checksum[i] != bb[bb.Length - 4 + i]) return null;
                }
            }

            byte[] rv = new byte[bb.Length - 4];
            System.Array.Copy(bb, 0, rv, 0, bb.Length - 4);
            return rv;
        }

        private string ByteArrayToString(byte[] ba)
        {
            return ByteArrayToString(ba, 0, ba.Length);
        }

        private string ByteArrayToString(byte[] ba, int offset, int count)
        {
            string rv = "";
            int usedcount = 0;
            for (int i = offset; usedcount < count; i++, usedcount++)
            {
                rv += string.Format("{0:X2}", ba[i]) + " ";
            }
            return rv;
        }

        private string ByteArrayToBase58(byte[] ba)
        {
            Org.BouncyCastle.Math.BigInteger addrremain = new Org.BouncyCastle.Math.BigInteger(1, ba);

            Org.BouncyCastle.Math.BigInteger big0 = new Org.BouncyCastle.Math.BigInteger("0");
            Org.BouncyCastle.Math.BigInteger big58 = new Org.BouncyCastle.Math.BigInteger("58");

            string b58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

            string rv = "";

            while (addrremain.CompareTo(big0) > 0)
            {
                int d = System.Convert.ToInt32(addrremain.Mod(big58).ToString());
                addrremain = addrremain.Divide(big58);
                rv = b58.Substring(d, 1) + rv;
            }

            // handle leading zeroes
            foreach (byte b in ba)
            {
                if (b != 0) break;
                rv = "1" + rv;

            }
            return rv;
        }


        private string ByteArrayToBase58Check(byte[] ba)
        {

            byte[] bb = new byte[ba.Length + 4];
            System.Array.Copy(ba, bb, ba.Length);

            //////////////////SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
            SHA256 sha256 = SHA256.Create();
            byte[] thehash = sha256.ComputeHash(ba);
            thehash = sha256.ComputeHash(thehash);
            for (int i = 0; i < 4; i++) bb[ba.Length + i] = thehash[i];
            return ByteArrayToBase58(bb);
        }

        private void button4_Click(object sender, System.EventArgs e)
        {
            Org.BouncyCastle.Asn1.X9.X9ECParameters ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
        }

        private byte[] GetHexBytes(string source, int minimum)
        {
            byte[] hex = GetHexBytes(source);
            if (hex == null) return null;
            // assume leading zeroes if we're short a few bytes
            if (hex.Length > (minimum - 6) && hex.Length < minimum)
            {
                byte[] hex2 = new byte[minimum];
                System.Array.Copy(hex, 0, hex2, minimum - hex.Length, hex.Length);
                hex = hex2;
            }
            // clip off one overhanging leading zero if present
            if (hex.Length == minimum + 1 && hex[0] == 0)
            {
                byte[] hex2 = new byte[minimum];
                System.Array.Copy(hex, 1, hex2, 0, minimum);
                hex = hex2;

            }

            return hex;
        }


        private byte[] GetHexBytes(string source)
        {
            System.Collections.Generic.List<byte> bytes = new System.Collections.Generic.List<byte>();
            // copy s into ss, adding spaces between each byte
            string s = source;
            string ss = "";
            int currentbytelength = 0;
            foreach (char c in s.ToCharArray())
            {
                if (c == ' ')
                {
                    currentbytelength = 0;
                }
                else
                {
                    currentbytelength++;
                    if (currentbytelength == 3)
                    {
                        currentbytelength = 1;
                        ss += ' ';
                    }
                }
                ss += c;
            }

            foreach (string b in ss.Split(' '))
            {
                int v = 0;
                if (b.Trim() == "") continue;
                foreach (char c in b.ToCharArray())
                {
                    if (c >= '0' && c <= '9')
                    {
                        v *= 16;
                        v += (c - '0');

                    }
                    else if (c >= 'a' && c <= 'f')
                    {
                        v *= 16;
                        v += (c - 'a' + 10);
                    }
                    else if (c >= 'A' && c <= 'F')
                    {
                        v *= 16;
                        v += (c - 'A' + 10);
                    }

                }
                v &= 0xff;
                bytes.Add((byte)v);
            }
            return bytes.ToArray();
        }

        private byte[] ValidateAndGetHexPrivateKey(byte leadingbyte)
        {
            byte[] hex = GetHexBytes(txtPrivHex.Text, 32);

            if (hex == null || hex.Length < 32 || hex.Length > 33)
            {
                MessageBox.Show("Hex is not 32 or 33 bytes.");
                return null;
            }

            // if leading 00, change it to 0x80
            if (hex.Length == 33)
            {
                if (hex[0] == 0 || hex[0] == 0x80)
                {
                    hex[0] = 0x80;
                }
                else
                {
                    MessageBox.Show("Not a valid private key");
                    return null;
                }
            }

            // add 0x80 byte if not present
            if (hex.Length == 32)
            {
                byte[] hex2 = new byte[33];
                System.Array.Copy(hex, 0, hex2, 1, 32);
                hex2[0] = 0x80;
                hex = hex2;
            }

            hex[0] = leadingbyte;
            return hex;

        }


        private byte[] ValidateAndGetHexPublicKey()
        {
            byte[] hex = GetHexBytes(txtPubHex.Text, 64);

            if (hex == null || hex.Length < 64 || hex.Length > 65)
            {
                MessageBox.Show("Hex is not 64 or 65 bytes.");
                return null;
            }

            // if leading 00, change it to 0x80
            if (hex.Length == 65)
            {
                if (hex[0] == 0 || hex[0] == 4)
                {
                    hex[0] = 4;
                }
                else
                {
                    MessageBox.Show("Not a valid public key");
                    return null;
                }
            }

            // add 0x80 byte if not present
            if (hex.Length == 64)
            {
                byte[] hex2 = new byte[65];
                System.Array.Copy(hex, 0, hex2, 1, 64);
                hex2[0] = 4;
                hex = hex2;
            }
            return hex;
        }

        private byte[] ValidateAndGetHexPublicHash()
        {
            byte[] hex = GetHexBytes(txtPubHash.Text, 20);

            if (hex == null || hex.Length != 20)
            {
                MessageBox.Show("Hex is not 20 bytes.");
                return null;
            }
            return hex;
        }


        private void btnPrivHexToWIF_Click(object sender, System.EventArgs e)
        {
            byte[] hex = ValidateAndGetHexPrivateKey(0x80);
            if (hex == null) return;
            txtPrivWIF.Text = ByteArrayToBase58Check(hex);
        }

        private void btnPrivWIFToHex_Click(object sender, System.EventArgs e)
        {
            byte[] hex = Base58ToByteArray(txtPrivWIF.Text);
            if (hex == null)
            {
                int L = txtPrivWIF.Text.Length;
                if (L >= 50 && L <= 52)
                {
                    if (MessageBox.Show("Private key is not valid.  Attempt to correct?", "Invalid address", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        CorrectWIF();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("WIF private key is not valid.");
                }
                return;
            }
            if (hex.Length != 33)
            {
                MessageBox.Show("WIF private key is not valid (wrong byte count, should be 33, was " + hex.Length + ")");
                return;
            }

            txtPrivHex.Text = ByteArrayToString(hex, 1, 32);


        }

        private void btnPrivToPub_Click(object sender, System.EventArgs e)
        {
            byte[] hex = ValidateAndGetHexPrivateKey(0x00);
            if (hex == null) return;

            Org.BouncyCastle.Asn1.X9.X9ECParameters ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            Org.BouncyCastle.Math.BigInteger Db = new Org.BouncyCastle.Math.BigInteger(hex);
            ECPoint dd = ps.G.Multiply(Db);

            byte[] pubaddr = new byte[65];
            byte[] Y = dd.Y.ToBigInteger().ToByteArray();
            System.Array.Copy(Y, 0, pubaddr, 64 - Y.Length + 1, Y.Length);
            byte[] X = dd.X.ToBigInteger().ToByteArray();
            System.Array.Copy(X, 0, pubaddr, 32 - X.Length + 1, X.Length);
            pubaddr[0] = 4;

            txtPubHex.Text = ByteArrayToString(pubaddr);

        }

        private void btnPubHexToHash_Click(object sender, System.EventArgs e)
        {
            byte[] hex = ValidateAndGetHexPublicKey();
            if (hex == null) return;

            //////////////SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
            SHA256 sha256 = SHA256.Create();
            byte[] shaofpubkey = sha256.ComputeHash(hex);

            RIPEMD160 rip = System.Security.Cryptography.RIPEMD160.Create();
            byte[] ripofpubkey = rip.ComputeHash(shaofpubkey);

            txtPubHash.Text = ByteArrayToString(ripofpubkey);

        }

        private void btnPubHashToAddress_Click(object sender, System.EventArgs e)
        {
            byte[] hex = ValidateAndGetHexPublicHash();
            if (hex == null) return;

            byte[] hex2 = new byte[21];
            System.Array.Copy(hex, 0, hex2, 1, 20);

            int cointype = 0;
            if (int.TryParse(cboCoinType.Text, out cointype) == false) cointype = 0;

            if (cboCoinType.Text == "Testnet") cointype = 111;
            if (cboCoinType.Text == "Namecoin") cointype = 52;
            hex2[0] = (byte)(cointype & 0xff);
            txtBtcAddr.Text = ByteArrayToBase58Check(hex2);

        }

        private void btnAddressToPubHash_Click(object sender, System.EventArgs e)
        {
            byte[] hex = Base58ToByteArray(txtBtcAddr.Text);
            if (hex == null || hex.Length != 21)
            {
                int L = txtBtcAddr.Text.Length;
                if (L >= 33 && L <= 34)
                {
                    if (MessageBox.Show("Address is not valid.  Attempt to correct?", "Invalid address", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        CorrectBitcoinAddress();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Address is not valid.");
                }
                return;
            }
            txtPubHash.Text = ByteArrayToString(hex, 1, 20);

        }

        private void btnGenerate_Click(object sender, System.EventArgs e)
        {
            ECKeyPairGenerator gen = new ECKeyPairGenerator();
            SecureRandom secureRandom = new SecureRandom();
            Org.BouncyCastle.Asn1.X9.X9ECParameters ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            ECDomainParameters ecParams = new ECDomainParameters(ps.Curve, ps.G, ps.N, ps.H);
            ECKeyGenerationParameters keyGenParam = new ECKeyGenerationParameters(ecParams, secureRandom);
            gen.Init(keyGenParam);

            AsymmetricCipherKeyPair kp = gen.GenerateKeyPair();

            ECPrivateKeyParameters priv = (ECPrivateKeyParameters)kp.Private;

            byte[] hexpriv = priv.D.ToByteArrayUnsigned();
            txtPrivHex.Text = ByteArrayToString(hexpriv);

            btnPrivHexToWIF_Click(null, null);
            btnPrivToPub_Click(null, null);
            btnPubHexToHash_Click(null, null);
            btnPubHashToAddress_Click(null, null);

        }

        private void btnBlockExplorer_Click(object sender, System.EventArgs e)
        {
            try
            {
                if (cboCoinType.Text == "Testnet")
                {
                    System.Diagnostics.Process.Start("http://www.blockexplorer.com/testnet/address/" + txtBtcAddr.Text);
                }
                else if (cboCoinType.Text == "Namecoin")
                {
                    System.Diagnostics.Process.Start("http://explorer.dot-bit.org/a/" + txtBtcAddr.Text);
                }
                else
                {
                    System.Diagnostics.Process.Start("http://www.blockexplorer.com/address/" + txtBtcAddr.Text);
                }
            }
            catch { }
        }

        private void CorrectBitcoinAddress()
        {
            txtBtcAddr.Text = Correction(txtBtcAddr.Text);
        }

        private string Correction(string btcaddr)
        {

            int btcaddrlen = btcaddr.Length;
            string b58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

            for (int i = 0; i < btcaddrlen; i++)
            {
                for (int j = 0; j < 58; j++)
                {
                    string attempt = btcaddr.Substring(0, i) + b58.Substring(j, 1) + btcaddr.Substring(i + 1);
                    byte[] bytes = Base58ToByteArray(attempt);
                    if (bytes != null)
                    {
                        MessageBox.Show("Correction was successful.  Try your request again.");
                        return attempt;
                    }
                }
            }
            return btcaddr;
        }

        private void CorrectWIF()
        {
            txtPrivWIF.Text = Correction(txtPrivWIF.Text);
        }
    }
}
