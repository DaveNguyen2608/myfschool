import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../controller/auth_controller.dart';

class ForgotPasswordPage extends StatefulWidget {
  const ForgotPasswordPage({super.key});

  @override
  State<ForgotPasswordPage> createState() => _ForgotPasswordPageState();
}

class _ForgotPasswordPageState extends State<ForgotPasswordPage> {
  final AuthController _authController = AuthController();

  final TextEditingController _phoneController = TextEditingController();
  final TextEditingController _otpController = TextEditingController();
  final TextEditingController _newPasswordController = TextEditingController();
  final TextEditingController _confirmPasswordController = TextEditingController();

  bool _isLoading = false;
  bool _otpSent = false;
  bool _obscureNewPassword = true;
  bool _obscureConfirmPassword = true;

  String? _maskedEmail;
  int _remainingSeconds = 0;
  Timer? _countdownTimer;

  @override
  void dispose() {
    _countdownTimer?.cancel();
    _phoneController.dispose();
    _otpController.dispose();
    _newPasswordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  bool _isValidVietnamPhone(String phone) {
    final regex = RegExp(r'^0[35789][0-9]{8}$');
    return regex.hasMatch(phone);
  }

  bool _isValidOtp(String otp) {
    final regex = RegExp(r'^[0-9]{6}$');
    return regex.hasMatch(otp);
  }

  void _startCountdown(int seconds) {
    _countdownTimer?.cancel();
    setState(() {
      _remainingSeconds = seconds;
    });

    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) {
        timer.cancel();
        return;
      }

      if (_remainingSeconds <= 1) {
        timer.cancel();
        setState(() {
          _remainingSeconds = 0;
        });
      } else {
        setState(() {
          _remainingSeconds -= 1;
        });
      }
    });
  }

  String _formatRemainingTime(int totalSeconds) {
    final minutes = (totalSeconds ~/ 60).toString().padLeft(2, '0');
    final seconds = (totalSeconds % 60).toString().padLeft(2, '0');
    return '$minutes:$seconds';
  }

  String _extractErrorMessage(Object error) {
    if (error is DioException) {
      final data = error.response?.data;
      if (data is Map<String, dynamic> && data['message'] != null) {
        return data['message'].toString();
      }
      return 'Không thể kết nối máy chủ';
    }
    return 'Đã có lỗi xảy ra';
  }

  Future<void> _handleSendOtp() async {
    final phone = _phoneController.text.trim();

    if (phone.isEmpty) {
      _showError('Vui lòng nhập số điện thoại');
      return;
    }

    if (!_isValidVietnamPhone(phone)) {
      _showError('Số điện thoại phải đúng định dạng Việt Nam (10 số)');
      return;
    }

    setState(() {
      _isLoading = true;
    });

    try {
      final response = await _authController.sendForgotPasswordOtp(phoneNumber: phone);
      final maskedEmail = response['maskedEmail']?.toString();
      final expiresIn = int.tryParse(response['expiresInSeconds']?.toString() ?? '') ?? 300;

      if (!mounted) {
        return;
      }

      setState(() {
        _otpSent = true;
        _maskedEmail = maskedEmail;
      });

      _startCountdown(expiresIn);
      _showSuccess(response['message']?.toString() ?? 'Đã gửi OTP thành công');
    } catch (error) {
      _showError(_extractErrorMessage(error));
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _handleResetPassword() async {
    final phone = _phoneController.text.trim();
    final otp = _otpController.text.trim();
    final newPassword = _newPasswordController.text;
    final confirmPassword = _confirmPasswordController.text;

    if (!_isValidVietnamPhone(phone)) {
      _showError('Số điện thoại không hợp lệ');
      return;
    }

    if (!_isValidOtp(otp)) {
      _showError('OTP phải gồm đúng 6 chữ số');
      return;
    }

    if (newPassword.length < 6) {
      _showError('Mật khẩu mới phải có ít nhất 6 ký tự');
      return;
    }

    if (newPassword != confirmPassword) {
      _showError('Xác nhận mật khẩu chưa khớp');
      return;
    }

    setState(() {
      _isLoading = true;
    });

    try {
      final message = await _authController.resetPasswordByOtp(
        phoneNumber: phone,
        otpCode: otp,
        newPassword: newPassword,
        confirmPassword: confirmPassword,
      );

      if (!mounted) {
        return;
      }

      _showSuccess(message);
      Navigator.pop(context);
    } catch (error) {
      _showError(_extractErrorMessage(error));
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  void _showError(String message) {
    if (!mounted) {
      return;
    }

    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(message),
          backgroundColor: Colors.red.shade600,
        ),
      );
  }

  void _showSuccess(String message) {
    if (!mounted) {
      return;
    }

    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(message),
          backgroundColor: Colors.green.shade600,
        ),
      );
  }

  Widget _buildPhoneInput() {
    return TextField(
      controller: _phoneController,
      keyboardType: TextInputType.phone,
      inputFormatters: [
        FilteringTextInputFormatter.digitsOnly,
        LengthLimitingTextInputFormatter(10),
      ],
      enabled: !_otpSent,
      decoration: InputDecoration(
        labelText: 'Số điện thoại',
        hintText: 'Nhập số điện thoại đã đăng ký',
        prefixIcon: const Icon(Icons.phone_android_rounded),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
        ),
      ),
    );
  }

  Widget _buildOtpAndPasswordForm() {
    return Column(
      children: [
        TextField(
          controller: _otpController,
          keyboardType: TextInputType.number,
          inputFormatters: [
            FilteringTextInputFormatter.digitsOnly,
            LengthLimitingTextInputFormatter(6),
          ],
          decoration: InputDecoration(
            labelText: 'Mã OTP',
            hintText: 'Nhập 6 chữ số OTP',
            prefixIcon: const Icon(Icons.password_rounded),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
            ),
          ),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _newPasswordController,
          obscureText: _obscureNewPassword,
          decoration: InputDecoration(
            labelText: 'Mật khẩu mới',
            hintText: 'Tối thiểu 6 ký tự',
            prefixIcon: const Icon(Icons.lock_outline_rounded),
            suffixIcon: IconButton(
              onPressed: () {
                setState(() {
                  _obscureNewPassword = !_obscureNewPassword;
                });
              },
              icon: Icon(
                _obscureNewPassword ? Icons.visibility_off : Icons.visibility,
              ),
            ),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
            ),
          ),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _confirmPasswordController,
          obscureText: _obscureConfirmPassword,
          decoration: InputDecoration(
            labelText: 'Xác nhận mật khẩu mới',
            hintText: 'Nhập lại mật khẩu mới',
            prefixIcon: const Icon(Icons.lock_reset_rounded),
            suffixIcon: IconButton(
              onPressed: () {
                setState(() {
                  _obscureConfirmPassword = !_obscureConfirmPassword;
                });
              },
              icon: Icon(
                _obscureConfirmPassword ? Icons.visibility_off : Icons.visibility,
              ),
            ),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
            ),
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    const orange = Color(0xFFF27024);

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: const Text('Quên mật khẩu'),
        centerTitle: true,
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const SizedBox(height: 8),
              const Text(
                'Khôi phục mật khẩu',
                style: TextStyle(
                  fontSize: 26,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                _otpSent
                    ? 'Mã OTP đã được gửi đến email: ${_maskedEmail ?? 'email đã đăng ký'}.'
                    : 'Nhập số điện thoại để nhận mã OTP qua email đã đăng ký trong hệ thống.',
                style: TextStyle(
                  color: Colors.grey.shade700,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 20),
              _buildPhoneInput(),
              const SizedBox(height: 12),
              if (_otpSent) ...[
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFFF4EA),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.schedule, color: orange, size: 18),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          _remainingSeconds > 0
                              ? 'OTP còn hiệu lực: ${_formatRemainingTime(_remainingSeconds)}'
                              : 'OTP đã hết hạn, vui lòng gửi lại mã mới',
                          style: TextStyle(
                            color: orange,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                _buildOtpAndPasswordForm(),
              ],
              const SizedBox(height: 20),
              SizedBox(
                height: 48,
                child: ElevatedButton(
                  onPressed: _isLoading
                      ? null
                      : (_otpSent ? _handleResetPassword : _handleSendOtp),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: orange,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  child: _isLoading
                      ? const SizedBox(
                          width: 22,
                          height: 22,
                          child: CircularProgressIndicator(
                            strokeWidth: 2.5,
                            color: Colors.white,
                          ),
                        )
                      : Text(
                          _otpSent ? 'Xác nhận đổi mật khẩu' : 'Gửi mã OTP',
                          style: const TextStyle(fontWeight: FontWeight.w700),
                        ),
                ),
              ),
              const SizedBox(height: 10),
              if (_otpSent)
                TextButton(
                  onPressed: _isLoading ? null : _handleSendOtp,
                  child: const Text('Gửi lại mã OTP'),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
