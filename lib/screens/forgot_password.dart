import 'package:animate_do/animate_do.dart';
import 'package:flutter/material.dart';

class ForgotPasswordPage extends StatefulWidget {
  const ForgotPasswordPage({super.key});

  @override
  State<ForgotPasswordPage> createState() => _ForgotPasswordPageState();
}

class _ForgotPasswordPageState extends State<ForgotPasswordPage> {
  final TextEditingController _phoneController = TextEditingController();
  final TextEditingController _otpController = TextEditingController();
  final FocusNode _otpFocusNode = FocusNode();

  bool _isOTPSent = false;

  @override
  void dispose() {
    _phoneController.dispose();
    _otpController.dispose();
    _otpFocusNode.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        elevation: 0,
        backgroundColor: Colors.transparent,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new, color: Colors.black, size: 20),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 25),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: 20),
              FadeInDown(
                duration: const Duration(milliseconds: 500),
                child: Center(
                  child: Image.asset(
                    'assets/images/Logo-Dai-hoc-FPT.png',
                    height: 180,
                    fit: BoxFit.contain,
                  ),
                ),
              ),
              const SizedBox(height: 40),
              FadeInLeft(
                duration: const Duration(milliseconds: 500),
                child: Text(
                  _isOTPSent ? "Xác thực OTP" : "Quên mật khẩu?",
                  style: const TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF1A1A1A),
                  ),
                ),
              ),
              const SizedBox(height: 10),
              FadeInLeft(
                duration: const Duration(milliseconds: 600),
                child: Text(
                  _isOTPSent
                      ? "Vui lòng nhập mã OTP đã được gửi đến số điện thoại của bạn."
                      : "Đừng lo lắng! Hãy nhập số điện thoại liên kết với tài khoản của bạn để nhận mã OTP.",
                  style: TextStyle(
                    fontSize: 16,
                    color: Colors.grey.shade600,
                    height: 1.5,
                  ),
                ),
              ),
              const SizedBox(height: 40),
              FadeInUp(
                duration: const Duration(milliseconds: 700),
                child: Column(
                  children: [
                    Container(
                      decoration: BoxDecoration(
                        color: Colors.grey.shade100,
                        borderRadius: BorderRadius.circular(15),
                        border: Border.all(color: Colors.grey.shade200),
                      ),
                      child: TextField(
                        controller: _isOTPSent ? _otpController : _phoneController,
                        focusNode: _isOTPSent ? _otpFocusNode : null,
                        keyboardType: _isOTPSent ? TextInputType.number : TextInputType.phone,
                        maxLength: _isOTPSent ? 6 : null,
                        decoration: InputDecoration(
                          counterText: "",
                          hintText: _isOTPSent ? "Mã OTP (6 số)" : "Số điện thoại",
                          hintStyle: TextStyle(color: Colors.grey.shade500),
                          border: InputBorder.none,
                          contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 18),
                          prefixIcon: Icon(
                            _isOTPSent ? Icons.lock_outline : Icons.phone_android_rounded,
                            color: const Color(0xFFF27024),
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(height: 30),
                    MaterialButton(
                      onPressed: () {
                        if (!_isOTPSent) {
                          setState(() {
                            _isOTPSent = true;
                            _otpController.clear();
                          });

                          Future.delayed(const Duration(milliseconds: 100), () {
                            if (mounted) {
                              FocusScope.of(context).requestFocus(_otpFocusNode);
                            }
                          });
                        } else {
                          // Handle OTP verification logic
                        }
                      },
                      height: 55,
                      minWidth: double.infinity,
                      color: const Color(0xFFF27024),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(15),
                      ),
                      child: Text(
                        _isOTPSent ? "Xác nhận mã OTP" : "Gửi mã OTP",
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 30),
              FadeInUp(
                duration: const Duration(milliseconds: 800),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      _isOTPSent ? "Chưa nhận được mã?" : "Quay lại ",
                      style: TextStyle(color: Colors.grey.shade600),
                    ),
                    TextButton(
                      onPressed: () {
                        if (_isOTPSent) {
                          _otpController.clear();
                          // Resend OTP logic
                        } else {
                          Navigator.pop(context);
                        }
                      },
                      child: Text(
                        _isOTPSent ? "Gửi lại ngay" : "Đăng nhập",
                        style: const TextStyle(
                          color: Color(0xFFF27024),
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}