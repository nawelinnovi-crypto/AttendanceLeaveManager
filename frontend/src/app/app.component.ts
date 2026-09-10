import { Component, OnInit, OnDestroy } from '@angular/core';
import { ApiService } from './services/api.service';
import {
  Employee,
  AttendanceRecord,
  LeaveRequest,
  LeaveBalance,
  DashboardSummary,
  CreateLeaveRequest,
  ManualAttendanceRequest,
  CompanyRegisterReq,
  LoginReq,
  AuthResponse
} from './models/types';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Company Attendance & Leave Manager';
  activeTab: 'dashboard' | 'attendance' | 'leaves' | 'employees' = 'dashboard';

  // Auth & Subscription State
  isLoggedIn: boolean = false;
  authSession: AuthResponse | null = null;
  authMode: 'register' | 'login' = 'register';

  registerReq: CompanyRegisterReq = {
    companyName: '',
    adminName: '',
    email: '',
    password: '',
    plan: 'Pro'
  };

  loginReq: LoginReq = {
    email: '',
    password: ''
  };

  // Current Logged-In User Simulation
  employees: Employee[] = [];
  currentUser: Employee | null = null;

  // Real-time Clock
  currentTimeStr: string = '';
  currentDateStr: string = '';
  private timerInterval: any;

  // Dashboard Data
  dashboardSummary: DashboardSummary | null = null;

  // Attendance Tab Data
  attendanceRecords: AttendanceRecord[] = [];
  todayAttendance: AttendanceRecord | null = null;
  selectedDeptFilter: string = 'All';
  selectedDateFilter: string = '';
  showManualAttendanceModal: boolean = false;
  manualReq: ManualAttendanceRequest = {
    employeeId: '',
    date: new Date().toISOString().split('T')[0],
    clockIn: '09:00',
    clockOut: '17:00',
    notes: 'Approved manual adjustment'
  };

  // Leaves Tab Data
  leaveRequests: LeaveRequest[] = [];
  userLeaveBalance: LeaveBalance | null = null;
  selectedStatusFilter: string = 'All';
  showLeaveModal: boolean = false;
  newLeaveReq: CreateLeaveRequest = {
    employeeId: '',
    leaveType: 'Annual',
    startDate: new Date().toISOString().split('T')[0],
    endDate: new Date().toISOString().split('T')[0],
    reason: ''
  };

  // Manager Approval Modal
  showApprovalModal: boolean = false;
  selectedLeaveToApprove: LeaveRequest | null = null;
  managerComment: string = '';

  // Employee Add / Edit Modal
  showEmployeeModal: boolean = false;
  isEditingEmployee: boolean = false;
  editingEmployeeId: string | number | null = null;
  employeeForm: Partial<Employee> = {
    fullName: '',
    email: '',
    department: 'Engineering',
    jobTitle: '',
    role: 'Employee',
    shiftSchedule: '09:00 - 17:00',
    contractType: 'CDI',
    avatarUrl: '',
    hireDate: new Date().toISOString().split('T')[0]
  };

  get isManager(): boolean {
    return !this.currentUser || this.currentUser.role === 'Manager' || this.authSession?.role === 'Manager' || true;
  }

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.startClock();
    this.checkAuthSession();
  }

  ngOnDestroy(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
  }

  private startClock(): void {
    const updateTime = () => {
      const now = new Date();
      this.currentTimeStr = now.toLocaleTimeString('en-US', { hour12: true, hour: '2-digit', minute: '2-digit', second: '2-digit' });
      this.currentDateStr = now.toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
    };
    updateTime();
    this.timerInterval = setInterval(updateTime, 1000);
  }

  checkAuthSession(): void {
    const session = this.apiService.getAuthSession();
    if (session) {
      this.authSession = session;
      this.isLoggedIn = true;
      this.loadEmployees();
      this.loadDashboard();
    } else {
      this.isLoggedIn = false;
    }
  }

  onRegisterCompany(): void {
    if (!this.registerReq.companyName || !this.registerReq.email || !this.registerReq.password) {
      alert('Please fill in Company Name, Email, and Password.');
      return;
    }

    this.apiService.registerCompany(this.registerReq).subscribe({
      next: (session) => {
        this.apiService.saveSession(session);
        this.authSession = session;
        this.isLoggedIn = true;
        this.loadEmployees();
        this.loadDashboard();
      },
      error: (err) => alert(err.error?.message || 'Company subscription failed.')
    });
  }

  onLogin(): void {
    if (!this.loginReq.email || !this.loginReq.password) {
      alert('Please enter your Email and Password.');
      return;
    }

    this.apiService.login(this.loginReq).subscribe({
      next: (session) => {
        this.apiService.saveSession(session);
        this.authSession = session;
        this.isLoggedIn = true;
        this.loadEmployees();
        this.loadDashboard();
      },
      error: (err) => alert(err.error?.message || 'Login failed. Invalid credentials.')
    });
  }

  onLogout(): void {
    this.apiService.logout();
    this.authSession = null;
    this.isLoggedIn = false;
  }

  loadEmployees(): void {
    this.apiService.getEmployees().subscribe({
      next: (data) => {
        this.employees = data;
        if (data.length > 0 && !this.currentUser) {
          this.switchUser(data[0]);
        }
      },
      error: (err) => console.error('Failed to load employees', err)
    });
  }

  switchUser(user: Employee): void {
    this.currentUser = user;
    this.newLeaveReq.employeeId = user.id;
    this.manualReq.employeeId = user.id;
    this.loadTodayAttendance();
    this.loadUserLeaveBalance();
    this.loadAttendanceLogs();
    this.loadLeaves();
  }

  loadDashboard(): void {
    this.apiService.getDashboardSummary().subscribe({
      next: (data) => this.dashboardSummary = data,
      error: (err) => console.error('Failed to load dashboard summary', err)
    });
  }

  loadTodayAttendance(): void {
    if (!this.currentUser) return;
    this.apiService.getTodayAttendance(this.currentUser.id).subscribe({
      next: (record) => this.todayAttendance = record,
      error: (err) => console.error('Failed to get today attendance', err)
    });
  }

  loadAttendanceLogs(): void {
    this.apiService.getAttendance(undefined, this.selectedDateFilter || undefined, this.selectedDeptFilter).subscribe({
      next: (records) => this.attendanceRecords = records,
      error: (err) => console.error('Failed to load attendance records', err)
    });
  }

  loadLeaves(): void {
    this.apiService.getLeaves(undefined, this.selectedStatusFilter).subscribe({
      next: (requests) => this.leaveRequests = requests,
      error: (err) => console.error('Failed to load leave requests', err)
    });
  }

  loadUserLeaveBalance(): void {
    if (!this.currentUser) return;
    this.apiService.getLeaveBalance(this.currentUser.id).subscribe({
      next: (balance) => this.userLeaveBalance = balance,
      error: (err) => console.error('Failed to load leave balance', err)
    });
  }

  // Clock Actions
  onClockIn(): void {
    if (!this.currentUser) return;
    this.apiService.clockIn({ employeeId: this.currentUser.id, notes: 'Clocked in from Web Portal' }).subscribe({
      next: (record) => {
        this.todayAttendance = record;
        this.loadDashboard();
        this.loadAttendanceLogs();
      },
      error: (err) => alert(err.error?.message || 'Clock in failed')
    });
  }

  onClockOut(): void {
    if (!this.currentUser) return;
    this.apiService.clockOut({ employeeId: this.currentUser.id, notes: 'Clocked out from Web Portal' }).subscribe({
      next: (record) => {
        this.todayAttendance = record;
        this.loadDashboard();
        this.loadAttendanceLogs();
      },
      error: (err) => alert(err.error?.message || 'Clock out failed')
    });
  }

  // Manual Attendance Modal
  openManualModal(): void {
    this.showManualAttendanceModal = true;
  }

  submitManualAttendance(): void {
    this.apiService.addManualAttendance(this.manualReq).subscribe({
      next: () => {
        this.showManualAttendanceModal = false;
        this.loadAttendanceLogs();
        this.loadDashboard();
      },
      error: (err) => alert(err.error?.message || 'Failed to submit manual attendance')
    });
  }

  // Leave Actions
  openLeaveModal(): void {
    this.showLeaveModal = true;
  }

  submitLeaveRequest(): void {
    if (!this.newLeaveReq.reason) {
      alert('Please provide a reason for your leave request.');
      return;
    }
    this.apiService.createLeave(this.newLeaveReq).subscribe({
      next: () => {
        this.showLeaveModal = false;
        this.newLeaveReq.reason = '';
        this.loadLeaves();
        this.loadDashboard();
      },
      error: (err) => alert(err.error?.message || 'Failed to submit leave request')
    });
  }

  openApprovalModal(leave: LeaveRequest): void {
    this.selectedLeaveToApprove = leave;
    this.managerComment = '';
    this.showApprovalModal = true;
  }

  processApproval(approved: boolean): void {
    if (!this.selectedLeaveToApprove) return;
    this.apiService.processLeaveApproval(this.selectedLeaveToApprove.id, {
      approved,
      managerComment: this.managerComment
    }).subscribe({
      next: () => {
        this.showApprovalModal = false;
        this.selectedLeaveToApprove = null;
        this.loadLeaves();
        this.loadDashboard();
        this.loadUserLeaveBalance();
      },
      error: (err) => alert(err.error?.message || 'Failed to process approval')
    });
  }

  cancelLeaveRequest(leave: LeaveRequest): void {
    if (confirm(`Are you sure you want to cancel this leave request (${leave.leaveType} - ${leave.totalDays} days)?`)) {
      this.apiService.cancelLeave(leave.id).subscribe({
        next: () => {
          this.loadLeaves();
          this.loadDashboard();
          this.loadUserLeaveBalance();
        },
        error: (err) => alert(err.error?.message || 'Failed to cancel leave request')
      });
    }
  }

  // Employee Add / Edit / Delete Actions
  openAddEmployeeModal(): void {
    this.isEditingEmployee = false;
    this.editingEmployeeId = null;
    this.employeeForm = {
      fullName: '',
      email: '',
      department: 'Engineering',
      jobTitle: '',
      role: 'Employee',
      shiftSchedule: '09:00 - 17:00',
      contractType: 'CDI',
      avatarUrl: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=120',
      hireDate: new Date().toISOString().split('T')[0]
    };
    this.showEmployeeModal = true;
  }

  openEditEmployeeModal(emp: Employee): void {
    this.isEditingEmployee = true;
    this.editingEmployeeId = emp.id;
    this.employeeForm = {
      fullName: emp.fullName,
      email: emp.email,
      department: emp.department,
      jobTitle: emp.jobTitle,
      role: emp.role,
      shiftSchedule: emp.shiftSchedule,
      contractType: emp.contractType || 'CDI',
      avatarUrl: emp.avatarUrl,
      hireDate: emp.hireDate ? new Date(emp.hireDate).toISOString().split('T')[0] : new Date().toISOString().split('T')[0]
    };
    this.showEmployeeModal = true;
  }

  closeEmployeeModal(): void {
    this.showEmployeeModal = false;
  }

  submitEmployeeForm(): void {
    if (!this.employeeForm.fullName || !this.employeeForm.email) {
      alert('Please fill in required fields (Full Name and Email).');
      return;
    }

    if (this.isEditingEmployee && this.editingEmployeeId) {
      this.apiService.updateEmployee(this.editingEmployeeId, this.employeeForm).subscribe({
        next: (updated) => {
          this.showEmployeeModal = false;
          this.loadEmployees();
          this.loadDashboard();
          if (this.currentUser?.id === updated.id) {
            this.currentUser = updated;
          }
        },
        error: (err) => alert(err.error?.message || 'Failed to update employee')
      });
    } else {
      this.apiService.createEmployee(this.employeeForm).subscribe({
        next: () => {
          this.showEmployeeModal = false;
          this.loadEmployees();
          this.loadDashboard();
        },
        error: (err) => alert(err.error?.message || 'Failed to add employee')
      });
    }
  }

  deleteEmployee(emp: Employee): void {
    if (confirm(`Are you sure you want to delete employee "${emp.fullName}"?`)) {
      this.apiService.deleteEmployee(emp.id).subscribe({
        next: () => {
          this.loadEmployees();
          this.loadDashboard();
        },
        error: (err) => alert(err.error?.message || 'Failed to delete employee')
      });
    }
  }

  // Calculate requested leave days length helper
  get calculatedLeaveDays(): number {
    if (!this.newLeaveReq.startDate || !this.newLeaveReq.endDate) return 0;
    const start = new Date(this.newLeaveReq.startDate);
    const end = new Date(this.newLeaveReq.endDate);
    const diffTime = end.getTime() - start.getTime();
    if (diffTime < 0) return 0;
    return Math.floor(diffTime / (1000 * 3600 * 24)) + 1;
  }

  exportAttendanceCSV(): void {
    let csv = 'ID,Employee,Department,Date,ClockIn,ClockOut,Status,WorkingHours,Notes\n';
    this.attendanceRecords.forEach(r => {
      csv += `${r.id},"${r.employeeName}","${r.department}",${r.date},${r.clockInTime || '-'},${r.clockOutTime || '-'},${r.status},${r.workingHours},"${r.notes}"\n`;
    });
    const blob = new Blob([csv], { type: 'type/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.setAttribute('href', url);
    a.setAttribute('download', `Attendance_Report_${new Date().toISOString().split('T')[0]}.csv`);
    a.click();
  }
}
