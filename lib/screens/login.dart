import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import '../controller/auth_controller.dart';
import 'forgot_password.dart';
import 'homescreen.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  bool _obscure = true;

  final _usernameCtrl = TextEditingController();
  final _passCtrl = TextEditingController();
  final AuthController _authController = AuthController();

  bool _isLoading = false;
  String? _error;

  @override
  void dispose() {
    _usernameCtrl.dispose();
    _passCtrl.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    final username = _usernameCtrl.text.trim();
    final password = _passCtrl.text.trim();

    if (username.isEmpty || password.isEmpty) {
      setState(() {
        _error = 'Vui lòng nhập đầy đủ tài khoản và mật khẩu';
      });
      return;
    }

    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final result = await _authController.login(
        username: username,
        password: password,
      );

      if (!mounted) return;

      Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (_) => HomeScreen(
            fullName: result.fullName,
            username: result.username,
          ),
        ),
      );
    } on DioException catch (e) {
      String errorMsg = 'Đăng nhập thất bại';

      final data = e.response?.data;
      if (data is Map<String, dynamic> && data.containsKey('message')) {
        errorMsg = data['message'].toString();
      } else if (e.type == DioExceptionType.connectionTimeout ||
          e.type == DioExceptionType.receiveTimeout ||
          e.type == DioExceptionType.sendTimeout) {
        errorMsg = 'Kết nối tới máy chủ bị timeout';
      } else if (e.type == DioExceptionType.connectionError) {
        errorMsg = 'Không kết nối được tới máy chủ';
      }

      if (!mounted) return;
      setState(() {
        _error = errorMsg;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = 'Có lỗi xảy ra';
      });
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  Widget _label(String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Text(
        text,
        style: const TextStyle(
          color: Colors.red,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  InputDecoration _underlineDec({
    String? hint,
    Widget? suffixIcon,
  }) {
    return InputDecoration(
      hintText: hint,
      hintStyle: const TextStyle(color: Colors.black),
      isDense: true,
      contentPadding: const EdgeInsets.symmetric(vertical: 10),
      suffixIcon: suffixIcon,
      enabledBorder: const UnderlineInputBorder(
        borderSide: BorderSide(color: Colors.black, width: 1),
      ),
      focusedBorder: const UnderlineInputBorder(
        borderSide: BorderSide(color: Colors.black, width: 1.2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 26, vertical: 18),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const SizedBox(height: 8),
              Center(
                child: Image.asset(
                  'assets/images/Thiết kế chưa có tên.png',
                  width: 200,
                  height: 200,
                  fit: BoxFit.contain,
                ),
              ),
              const SizedBox(height: 26),
              const Text(
                "Sign In",
                style: TextStyle(
                  fontSize: 30,
                  fontWeight: FontWeight.w800,
                  color: Colors.orange,
                ),
              ),
              const SizedBox(height: 6),
              const Text(
                "Hi there! Nice to see you again.",
                style: TextStyle(color: Colors.orange, fontSize: 14),
              ),
              const SizedBox(height: 26),

              _label("Username:"),
              TextField(
                controller: _usernameCtrl,
                decoration: _underlineDec(hint: ""),
              ),

              const SizedBox(height: 18),

              _label("Password"),
              TextField(
                controller: _passCtrl,
                obscureText: _obscure,
                decoration: _underlineDec(
                  hint: "",
                  suffixIcon: IconButton(
                    onPressed: () => setState(() => _obscure = !_obscure),
                    icon: Icon(
                      _obscure ? Icons.visibility_off : Icons.visibility,
                      color: Colors.black26,
                    ),
                  ),
                ),
              ),

              const SizedBox(height: 12),
              if (_error != null)
                Text(
                  _error!,
                  style: const TextStyle(color: Colors.red),
                ),

              const SizedBox(height: 16),
              SizedBox(
                height: 48,
                child: ElevatedButton(
                  onPressed: _isLoading ? null : _handleLogin,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFFFF6D2D),
                    elevation: 0,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(6),
                    ),
                  ),
                  child: _isLoading
                      ? const SizedBox(
                          width: 24,
                          height: 24,
                          child: CircularProgressIndicator(
                            color: Colors.white,
                            strokeWidth: 2.5,
                          ),
                        )
                      : const Text(
                          "Sign in",
                          style: TextStyle(fontWeight: FontWeight.w700),
                        ),
                ),
              ),

              const SizedBox(height: 26),

              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  GestureDetector(
                    onTap: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) => const ForgotPasswordPage(),
                        ),
                      );
                    },
                    child: const Text(
                      "Forgot Password?",
                      style: TextStyle(
                        color: Colors.black38,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  const SizedBox(),
                ],
              ),

              const SizedBox(height: 10),
              const Padding(
                padding: EdgeInsets.only(bottom: 10),
                child: Text(
                  "Copyright by TienNHHE182008",
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: Colors.grey,
                    fontSize: 12,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}